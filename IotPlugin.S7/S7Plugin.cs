using System.Collections.Concurrent;
using System.Globalization;
using CenBoCommon.Zxx;
using CenboEventBus;
using IotDriverCore;
using IotLog;
using IotModel;
using NewLife.Threading;
using S7.Net;

namespace IotPlugin.S7
{
    /// <summary>
    /// 西门子S7采集插件(M3.6:S7netplus平台直连型驱动,支持S7-200Smart/300/400/1200/1500;
    /// 每设备独立采集循环+ReconnectBackoff退避重连(库自带IO不经指令引擎);
    /// 点表约定:CollectFuncCode 1=DB/2=M/3=I/4=Q区,ParamAddr=DB号×1000000+字节地址(M/I/Q区DB=0),
    /// CollectDataType bool/byte/int16/uint16/int32/uint32/float32(S7大端),CollectBitOffset位偏移;
    /// 按(区,DB)分组经PointBatchBuilder连续性合包批量读;
    /// 写下发nets7write:CollectWritable点位入每设备写队列由采集循环串行消费)
    /// </summary>
    public class S7Plugin : ICenBoPlugin, ISimulatable
    {
        // ===== 模拟人格(方案A:独立端口/生命周期,委托S7Simulator;从站编码手写与库客户端独立) =====
        private readonly Sim.S7Simulator _simulator = new();

        public SimCapability Capability => _simulator.Capability;
        public Action<SimLogEntry>? OnSimLog
        {
            get => _simulator.OnSimLog;
            set => _simulator.OnSimLog = value;
        }
        public Task<SimStatus> StartSimAsync(SimStartRequest request, CancellationToken ct) => _simulator.StartSimAsync(request, ct);
        public Task StopSimAsync(string simId) => _simulator.StopSimAsync(simId);
        public IReadOnlyList<SimStatus> ListSims() => _simulator.ListSims();
        public Task InjectFaultAsync(string simId, SimFaultSpec fault) => _simulator.InjectFaultAsync(simId, fault);

        public bool OwnsDeviceType(string deviceTypeCode) =>
            _config != null && _config.DeviceTypeCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(deviceTypeCode, StringComparer.OrdinalIgnoreCase);

        private IEventBus<PluginEvent>? _eventBus;
        private S7PluginConfig? _config;
        private TimerX? _heartTimer;
        private CancellationTokenSource? _cts;

        private readonly object _bindingLock = new();

        /// <summary>设备ID→绑定</summary>
        private Dictionary<int, S7DeviceBinding> _deviceMap = new();

        /// <summary>
        /// S7设备绑定(一台PLC一条TCP直连)
        /// </summary>
        private class S7DeviceBinding
        {
            public DeviceInfo Device = null!;
            public DeviceParamEntity? DeviceParam;
            public List<DeviceTypeParam> Points = new();
            public CpuType Cpu = CpuType.S71200;
            public volatile bool Online;

            /// <summary>待写请求队列(Plc实例非线程安全,写必须由本设备采集循环串行消费)</summary>
            public readonly ConcurrentQueue<S7WriteRequest> WriteQueue = new();
        }

        public void PluginInit(IEventBus<PluginEvent> eventBus) => _eventBus = eventBus;

        public string PluginName => "S7Plugin";
        public string PluginType => "西门子S7采集插件";
        public string PluginDesc => "西门子S7系列PLC批量采集(基于IotDriverCore驱动框架,S7netplus)";
        public string PluginVersion => "1.0.0";
        public string PluginGuid => "e1f7a3b9c5d6840e7fa18b9c2345e678";
        public string PluginModelPath => "";

        /// <summary>
        /// 插件自描述清单(B-1.3:配置schema+缺省配置+控制命令清单+寻址说明,
        /// 宿主在上传/加载时持久化到sys_plugin.plugin_manifest)
        /// </summary>
        public string PluginManifest => PluginMetaBuilder.BuildManifest(
            S7PluginConfig.Current,
            new[]
            {
                new PluginMetaBuilder.PluginCommandMeta("nets7write", "S7写点位(按参数编码定位点表,CollectWritable点位入每设备写队列由采集循环串行消费)")
            },
            "点表寻址:CollectFuncCode 1=DB/2=M/3=I/4=Q区,ParamAddr=DB号×1000000+字节地址,CollectBitOffset位偏移;端点=DeviceIp:DevicePort,CPU型号按类型编码映射",
            "s7");

        #region 启动/停止

        /// <summary>
        /// 解析插件配置(B-1.1配置单轨化:DB plugin_config为唯一事实源;
        /// 传入为空时回落本地Config文件仅作首次迁移来源,JSON解析失败返回null由启动失败兜底)
        /// </summary>
        private S7PluginConfig? LoadConfig(string _PluginConfig)
        {
            if (_PluginConfig.IsZxxNullOrEmpty()) return S7PluginConfig.Current;
            try
            {
                return _PluginConfig.ToObject<S7PluginConfig>();
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("S7Plugin", "LoadConfig", ex.ToString(), "S7插件");
                return null;
            }
        }

        /// <summary>
        /// 启动插件:校验配置→加载绑定与点表→每设备拉起独立采集循环→启动心跳
        /// </summary>
        public async Task<bool> PluginStart(string _PluginConfig)
        {
            try
            {
                _config = LoadConfig(_PluginConfig);
                if (_config == null)
                {
                    LogHelper.SysLogWrite("S7Plugin", "PluginStart", $"{PluginName}：配置加载失败，插件启动失败。", "S7插件");
                    return false;
                }
                if (_config.DeviceTypeCodes.IsZxxNullOrEmpty())
                {
                    LogHelper.SysLogWrite("S7Plugin", "PluginStart", $"{PluginName}：未配置设备类型编码，插件不启用。", "S7插件");
                    return false;
                }
                if (_config.CollectCycleMs <= 0 || _config.MaxBatchBytes <= 0)
                {
                    LogHelper.SysLogWrite("S7Plugin", "PluginStart", $"{PluginName}：配置参数存在非正数，插件启动失败。", "S7插件");
                    return false;
                }
                if (!RefreshBindings(out var err))
                {
                    LogHelper.SysLogWrite("S7Plugin", "PluginStart", $"{PluginName}：初始化设备失败，{err}", "S7插件");
                    return false;
                }

                _cts = new CancellationTokenSource();
                List<S7DeviceBinding> bindings;
                lock (_bindingLock) { bindings = _deviceMap.Values.ToList(); }
                foreach (var binding in bindings)
                {
                    var b = binding;
                    _ = Task.Run(() => RunDeviceLoopAsync(b, _cts.Token));
                }

                _heartTimer?.Dispose();
                _heartTimer = new TimerX(HeartBeatDown, null, 5_000, _config.HeartSecond * 1_000);
                LogHelper.SysLogWrite("S7Plugin", "PluginStart", $"{PluginName}：插件启动成功，{bindings.Count}台PLC。", "S7插件");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("S7Plugin", "PluginStart", ex.ToString(), "S7插件");
                return false;
            }
        }

        /// <summary>
        /// 停止插件:销毁心跳→取消所有采集循环→清空绑定
        /// </summary>
        public async Task<bool> PluginStop()
        {
            try
            {
                _heartTimer?.Dispose();
                _cts?.Cancel();
                _cts = null;
                lock (_bindingLock) { _deviceMap = new Dictionary<int, S7DeviceBinding>(); }
                _simulator.StopAll();
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("S7Plugin", "PluginStop", ex.ToString(), "S7插件"); }
            return true;
        }

        #endregion

        #region 心跳

        /// <summary>
        /// 心跳定时器回调(TimerX回调不支持async,故用Wait)
        /// </summary>
        private void HeartBeatDown(object? _)
        {
            SendMessageAsync(new PluginMessage
            {
                MessageType = PluginMessageEnum.心跳,
                MessageJson = new { Message = $"{PluginName}心跳", Time = DateTime.Now }.ToJson()
            }).Wait();
        }

        /// <summary>
        /// 通过事件总线向主程序发布消息(异常只记日志不上抛)
        /// </summary>
        public async Task SendMessageAsync(PluginMessage mess)
        {
            try { _eventBus?.Publish(new PluginEvent(PluginGuid, mess)); }
            catch (Exception ex) { LogHelper.ErrorLogWrite("S7Plugin", "SendMessageAsync", ex.ToString(), "S7插件"); }
        }

        #endregion

        #region 设备初始化

        /// <summary>
        /// 从数据库加载PLC绑定与点表并原子替换内存映射(点表条件:CollectFuncCode为1~4区)
        /// </summary>
        private bool RefreshBindings(out string error)
        {
            error = "";
            var typecodes = _config!.DeviceTypeCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var cpumap = ParseCpuTypeMap(_config.CpuTypeMap);
            var devices = DeviceInfoDAO.Instance.GetListBy(t => typecodes.Contains(t.DeviceTypeCode) && t.IsCollection == 1);
            if (!devices.IsZxxAny())
            {
                error = "未查询到归属本插件的采集设备。";
                return false;
            }

            var allpoints = DeviceTypeParamDAO.Instance.GetList()?.Cast<DeviceTypeParam>().ToList() ?? new List<DeviceTypeParam>();
            var pointmap = allpoints
                .Where(t => typecodes.Contains(t.DeviceTypeCode) && t.CollectFuncCode >= 1 && t.CollectFuncCode <= 4)
                .GroupBy(t => t.DeviceTypeCode)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var deviceids = devices.Select(t => t.DeviceId).ToList();
            var devparams = DeviceParamDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));

            var devicemap = new Dictionary<int, S7DeviceBinding>();
            foreach (var device in devices)
            {
                if (device.DeviceIp.IsZxxNullOrEmpty())
                {
                    LogHelper.SysLogWrite("S7Plugin", "RefreshBindings", $"{PluginName}：设备[{device.DeviceId}({device.DeviceName})]未配置IP，跳过。", "S7插件");
                    continue;
                }
                if (!pointmap.TryGetValue(device.DeviceTypeCode, out var points) || !points.IsZxxAny())
                {
                    LogHelper.SysLogWrite("S7Plugin", "RefreshBindings", $"{PluginName}：类型[{device.DeviceTypeCode}]无S7点表配置，设备[{device.DeviceId}]跳过。", "S7插件");
                    continue;
                }
                devicemap[device.DeviceId] = new S7DeviceBinding
                {
                    Device = device,
                    DeviceParam = devparams?.Find(t => t.DeviceId == device.DeviceId),
                    Points = points,
                    Cpu = cpumap.TryGetValue(device.DeviceTypeCode, out var cpu) ? cpu : CpuType.S71200
                };
            }
            if (!devicemap.Any())
            {
                error = "未构建任何有效PLC绑定。";
                return false;
            }

            lock (_bindingLock) { _deviceMap = devicemap; }
            LogHelper.SysLogWrite("S7Plugin", "RefreshBindings", $"{PluginName}：PLC映射初始化完成，{devicemap.Count}台。", "S7插件");
            return true;
        }

        /// <summary>
        /// 解析类型编码→CPU型号映射("类型编码:S71200"逗号分隔)
        /// </summary>
        private static Dictionary<string, CpuType> ParseCpuTypeMap(string config)
        {
            var result = new Dictionary<string, CpuType>(StringComparer.OrdinalIgnoreCase);
            if (config.IsZxxNullOrEmpty()) return result;
            foreach (var pair in config.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = pair.Split(':', StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && Enum.TryParse<CpuType>(parts[1], true, out var cpu)) result[parts[0]] = cpu;
            }
            return result;
        }

        #endregion

        #region 采集循环

        /// <summary>
        /// 单台PLC采集主循环:连接(退避重连)→按周期合包批量读→断线发布离线
        /// </summary>
        private async Task RunDeviceLoopAsync(S7DeviceBinding binding, CancellationToken token)
        {
            var backoff = new ReconnectBackoff();
            var batches = BuildBatches(binding);
            if (!batches.Any())
            {
                LogHelper.SysLogWrite("S7Plugin", "RunDeviceLoopAsync", $"{PluginName}：设备[{binding.Device.DeviceId}]无有效采集批次。", "S7插件");
                return;
            }
            while (!token.IsCancellationRequested)
            {
                Plc? plc = null;
                try
                {
                    plc = new Plc(binding.Cpu, binding.Device.DeviceIp,
                        binding.Device.DevicePort > 0 ? binding.Device.DevicePort : 102,
                        _config!.Rack, _config.Slot);
                    await plc.OpenAsync(token);
                    backoff.Reset();
                    binding.Online = true;
                    LogHelper.SysLogWrite("S7Plugin", "RunDeviceLoopAsync", $"{PluginName}：PLC[{binding.Device.DeviceId}({binding.Device.DeviceIp})]连接建立。", "S7插件");
                    PublishRunState(binding, 2);

                    while (!token.IsCancellationRequested && plc.IsConnected)
                    {
                        await DrainWriteQueueAsync(binding, plc, token);
                        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var batch in batches)
                        {
                            var (datatype, db) = DecodeArea(batch);
                            var buffer = await plc.ReadBytesAsync(datatype, db, batch.StartAddress % 1_000_000, batch.TotalLength, token);
                            foreach (var point in batch.Points)
                            {
                                var value = DecodePoint(buffer, point, batch.StartAddress % 1_000_000);
                                if (value != null) values[point.ParamCode] = value;
                            }
                        }
                        if (values.Any())
                        {
                            var data = BuildDeviceData(binding, values);
                            if (data != null)
                            {
                                await SendMessageAsync(new PluginMessage
                                {
                                    MessageType = PluginMessageEnum.协议解析,
                                    MessageJson = new List<DeviceData> { data }.ToJson()
                                });
                            }
                        }
                        await Task.Delay(_config.CollectCycleMs, token);
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    LogHelper.SysLogWrite("S7Plugin", "RunDeviceLoopAsync", $"{PluginName}：PLC[{binding.Device.DeviceId}]通信异常，{ex.Message}", "S7插件");
                }
                finally
                {
                    if (binding.Online)
                    {
                        binding.Online = false;
                        LogHelper.SysLogWrite("S7Plugin", "RunDeviceLoopAsync", $"{PluginName}：PLC[{binding.Device.DeviceId}]连接断开。", "S7插件");
                        PublishRunState(binding, 0);
                    }
                    try { plc?.Close(); } catch { }
                    FailPendingWrites(binding, "PLC连接断开，写请求未执行");
                }
                if (token.IsCancellationRequested) break;
                try { await Task.Delay(backoff.NextDelayMs(), token); }
                catch (OperationCanceledException) { break; }
            }
        }

        /// <summary>
        /// 点表按(区,DB)分组经PointBatchBuilder连续性合包(字节粒度)
        /// </summary>
        private List<PointBatch> BuildBatches(S7DeviceBinding binding)
        {
            var driverpoints = binding.Points.Select(t => new DriverPoint
            {
                DeviceId = binding.Device.DeviceId,
                ParamCode = t.ParamCode,
                SlaveAddr = t.ParamAddr / 1_000_000,   // DB号充当分组键
                FuncCode = (byte)t.CollectFuncCode,
                Address = t.ParamAddr,                 // 含DB号编码,同DB内连续性不受影响
                Length = ByteLength(t),
                DataType = t.CollectDataType ?? "",
                ByteOrder = "",
                BitOffset = t.CollectBitOffset
            }).ToList();
            return PointBatchBuilder.Build(driverpoints, _config!.MaxBatchBytes, _config.GapTolerance);
        }

        /// <summary>
        /// 批次(区,DB)解码(FuncCode 1=DB/2=M/3=I/4=Q)
        /// </summary>
        private static (DataType Area, int Db) DecodeArea(PointBatch batch) =>
            (batch.FuncCode switch
            {
                2 => DataType.Memory,
                3 => DataType.Input,
                4 => DataType.Output,
                _ => DataType.DataBlock
            }, batch.SlaveAddr);

        /// <summary>
        /// 按数据类型推导占用字节数
        /// </summary>
        private static int ByteLength(DeviceTypeParam point)
        {
            if (point.CollectRegLength > 0) return point.CollectRegLength;
            return (point.CollectDataType ?? "").Trim().ToLowerInvariant() switch
            {
                "bool" or "bit" or "byte" => 1,
                "int32" or "uint32" or "float32" => 4,
                _ => 2
            };
        }

        /// <summary>
        /// 从批次缓冲切片解码单点(S7大端;bool按位偏移取位)
        /// </summary>
        private static string? DecodePoint(byte[] buffer, DriverPoint point, int batchstart)
        {
            int offset = point.Address % 1_000_000 - batchstart;
            int length = Math.Max(1, point.Length);
            if (offset < 0 || offset + length > buffer.Length) return null;
            var type = (point.DataType ?? "").Trim().ToLowerInvariant();
            switch (type)
            {
                case "bool":
                case "bit":
                    return ((buffer[offset] >> Math.Max(0, point.BitOffset)) & 1).ToString();
                case "byte":
                    return buffer[offset].ToString(CultureInfo.InvariantCulture);
                case "int16":
                    return ((short)((buffer[offset] << 8) | buffer[offset + 1])).ToString(CultureInfo.InvariantCulture);
                case "int32":
                    return ((int)ReadU32(buffer, offset)).ToString(CultureInfo.InvariantCulture);
                case "uint32":
                    return ReadU32(buffer, offset).ToString(CultureInfo.InvariantCulture);
                case "float32":
                    return BitConverter.Int32BitsToSingle((int)ReadU32(buffer, offset)).ToString("R", CultureInfo.InvariantCulture);
                default:
                    return ((ushort)((buffer[offset] << 8) | buffer[offset + 1])).ToString(CultureInfo.InvariantCulture);
            }
        }

        private static uint ReadU32(byte[] buffer, int offset) =>
            (uint)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);

        #endregion

        #region 写下发

        /// <summary>
        /// 冲刷写队列(采集循环每轮先写后读,保证写优先;单条失败只回执不断连)
        /// </summary>
        private async Task DrainWriteQueueAsync(S7DeviceBinding binding, Plc plc, CancellationToken token)
        {
            while (!token.IsCancellationRequested && binding.WriteQueue.TryDequeue(out var request))
            {
                bool success = false;
                string message;
                try
                {
                    var buffer = EncodeWriteValue(request.Point, request.ParamValue);
                    if (buffer == null)
                    {
                        message = "值编码失败或类型不支持写入";
                    }
                    else
                    {
                        var (area, db) = request.Point.CollectFuncCode switch
                        {
                            2 => (DataType.Memory, 0),
                            3 => (DataType.Input, 0),
                            4 => (DataType.Output, 0),
                            _ => (DataType.DataBlock, request.Point.ParamAddr / 1_000_000)
                        };
                        int start = request.Point.ParamAddr % 1_000_000;
                        var type = (request.Point.CollectDataType ?? "").Trim().ToLowerInvariant();
                        if (type is "bool" or "bit")
                        {
                            // 位写走WriteBitAsync,避免读改写整字节竞态
                            bool bit = request.ParamValue.Trim() is "1" or "true" or "True" or "TRUE";
                            await plc.WriteBitAsync(area, db, start, Math.Max(0, request.Point.CollectBitOffset), bit, token);
                        }
                        else
                        {
                            await plc.WriteBytesAsync(area, db, start, buffer, token);
                        }
                        success = true;
                        message = "写入成功";
                        LogHelper.SysLogWrite("S7Plugin", "DrainWriteQueueAsync", $"{PluginName}：PLC[{binding.Device.DeviceId}]写点位[{request.Point.ParamCode}]值[{request.ParamValue}]成功。", "S7插件");
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    message = $"写入异常：{ex.Message}";
                    LogHelper.SysLogWrite("S7Plugin", "DrainWriteQueueAsync", $"{PluginName}：PLC[{binding.Device.DeviceId}]写点位[{request.Point.ParamCode}]失败，{ex.Message}", "S7插件");
                }
                await PublishControlResultAsync(request.CommandId, binding.Device.DeviceId, binding.Device.DeviceName, success, message);
            }
        }

        /// <summary>
        /// 工程值字符串按点表数据类型编码为S7大端字节(bool由位写单独处理返回占位)
        /// </summary>
        private static byte[]? EncodeWriteValue(DeviceTypeParam point, string value)
        {
            var type = (point.CollectDataType ?? "").Trim().ToLowerInvariant();
            value = value?.Trim() ?? "";
            switch (type)
            {
                case "bool":
                case "bit":
                    return new byte[1];
                case "byte":
                    return byte.TryParse(value, out var b) ? new[] { b } : null;
                case "int16":
                    return short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i16)
                        ? new[] { (byte)(i16 >> 8), (byte)i16 } : null;
                case "int32":
                    return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i32)
                        ? WriteU32((uint)i32) : null;
                case "uint32":
                    return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var u32)
                        ? WriteU32(u32) : null;
                case "float32":
                    return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f32)
                        ? WriteU32((uint)BitConverter.SingleToInt32Bits(f32)) : null;
                default:
                    return ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var u16)
                        ? new[] { (byte)(u16 >> 8), (byte)u16 } : null;
            }
        }

        private static byte[] WriteU32(uint value) =>
            new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };

        /// <summary>
        /// 断线清空写队列并逐条发失败回执
        /// </summary>
        private void FailPendingWrites(S7DeviceBinding binding, string reason)
        {
            while (binding.WriteQueue.TryDequeue(out var request))
            {
                _ = PublishControlResultAsync(request.CommandId, binding.Device.DeviceId, binding.Device.DeviceName, false, reason);
            }
        }

        /// <summary>
        /// 发布控制结果消息
        /// </summary>
        private async Task PublishControlResultAsync(string commandid, int deviceid, string devicename, bool success, string message)
        {
            if (commandid.IsZxxNullOrEmpty()) return;
            var result = new PluginControlResultMessage
            {
                CommandId = commandid,
                ResultTime = DateTime.Now.ToDateTimeString(),
                DeviceResults = new List<ControlDeviceResult>
                {
                    new ControlDeviceResult
                    {
                        DeviceId = deviceid,
                        DeviceName = devicename,
                        Success = success,
                        Message = message,
                        ResultTime = DateTime.Now.ToDateTimeString()
                    }
                }
            };
            await SendMessageAsync(new PluginMessage
            {
                MessageType = PluginMessageEnum.控制结果,
                MessageJson = result.ToJson()
            });
        }

        #endregion

        #region 数据发布

        /// <summary>
        /// 按设备参数模板构建DeviceData(套用ParamFormula公式,按最大/最小合法值标记IsAlarm)
        /// </summary>
        private DeviceData? BuildDeviceData(S7DeviceBinding binding, Dictionary<string, string> values)
        {
            string timestr = DateTime.Now.ToDateTimeString();
            var dev = new DeviceInfoEntity();
            binding.Device.CopyTypeValue(dev);
            dev.LastOnlineTime = timestr;
            dev.DeviceState = 2;

            if (binding.DeviceParam == null || !binding.DeviceParam.ExpandObjects.IsZxxAny())
            {
                return new DeviceData
                {
                    DeviceId = dev.DeviceId,
                    device = dev,
                    deviceparam = new List<Expand_DeviceParam>(),
                    paramtype = 0
                };
            }

            var realparams = new List<Expand_DeviceParam>();
            foreach (var tmpl in binding.DeviceParam.ExpandObjects)
            {
                if (!values.TryGetValue(tmpl.ParamCode, out var raw)) continue;

                var p = new Expand_DeviceParam();
                tmpl.CopyTypeValue(p);
                p.StatusValues = tmpl.StatusValues;
                p.CollectTime = timestr;
                p.ParamLastValue = tmpl.ParamValue;
                p.ParamValue = raw;

                if (!p.ParamFormula.IsZxxNullOrEmpty()
                    && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double numeric))
                {
                    string calc = ExpressoFormula.CalculateString(p.ParamFormula, p.ParamCode, numeric, p.DecimalDigit);
                    if (!calc.IsZxxNullOrEmpty()) p.ParamValue = calc;
                }

                p.IsAlarm = 0;
                if (p.ParamMinValue != 0 || p.ParamMaxValue != 0)
                {
                    decimal pv = p.ParamValue.ToDecimal();
                    if (pv < p.ParamMinValue || pv > p.ParamMaxValue) p.IsAlarm = 1;
                }
                realparams.Add(p);
            }
            if (!realparams.IsZxxAny()) return null;
            return new DeviceData
            {
                DeviceId = dev.DeviceId,
                device = dev,
                deviceparam = realparams,
                paramtype = 0
            };
        }

        /// <summary>
        /// 发布单台PLC运行状态(2在线/0离线)
        /// </summary>
        private void PublishRunState(S7DeviceBinding binding, int state)
        {
            try
            {
                var dev = new DeviceInfoEntity();
                binding.Device.CopyTypeValue(dev);
                dev.DeviceState = state;
                _ = SendMessageAsync(new PluginMessage
                {
                    MessageType = PluginMessageEnum.运行状态,
                    MessageJson = new List<DeviceData>
                    {
                        new DeviceData
                        {
                            DeviceId = dev.DeviceId,
                            device = dev,
                            deviceparam = new List<Expand_DeviceParam>(),
                            paramtype = 0
                        }
                    }.ToJson()
                });
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("S7Plugin", "PublishRunState", ex.ToString(), "S7插件"); }
        }

        #endregion

        #region 接收主程序消息

        /// <summary>
        /// 接收主程序消息入口(心跳/设备控制)
        /// </summary>
        public async Task ReceiveMessageAsync(PluginMessage mess)
        {
            switch (mess.MessageType)
            {
                case PluginMessageEnum.心跳:
                    LogHelper.SysLogWrite("S7Plugin", "ReceiveMessageAsync", $"{PluginName}：收到心跳。", "S7插件");
                    break;
                case PluginMessageEnum.设备控制:
                    await HandleDeviceControlAsync(mess.MessageJson);
                    break;
                case PluginMessageEnum.配置更新:
                    await RestartForConfigUpdateAsync();
                    break;
            }
        }

        private readonly SemaphoreSlim _restartGate = new(1, 1);
        private int _restartPending;

        /// <summary>
        /// 配置更新自重启(C-4:设备/点表/通道拓扑变更后全量重建;PluginStart无防重入护栏须闸门串行,
        /// 重启期间再次到达的通知置位合并,由当前循环收尾补跑,不丢末次变更)
        /// </summary>
        private async Task RestartForConfigUpdateAsync()
        {
            Interlocked.Exchange(ref _restartPending, 1);
            if (!await _restartGate.WaitAsync(0)) return;
            try
            {
                while (Interlocked.Exchange(ref _restartPending, 0) == 1)
                {
                    LogHelper.SysLogWrite("S7Plugin", "RestartForConfigUpdateAsync", $"{PluginName}：收到配置更新，重建采集拓扑。", "S7插件");
                    await PluginStop();
                    await PluginStart(_config?.ToJson() ?? "");
                }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("S7Plugin", "RestartForConfigUpdateAsync", ex.ToString(), "S7插件"); }
            finally { _restartGate.Release(); }
        }

        /// <summary>
        /// 处理写点位控制(nets7write:按参数编码定位点表,入每设备写队列由采集循环串行消费——
        /// Plc实例非线程安全,不可在消息线程直接写)
        /// </summary>
        private async Task HandleDeviceControlAsync(string? messagejson)
        {
            if (messagejson.IsZxxNullOrEmpty())
            {
                LogHelper.SysLogWrite("S7Plugin", "HandleDeviceControlAsync", $"{PluginName}：设备控制消息为空。", "S7插件");
                return;
            }
            S7ControlCommand? command;
            try { command = messagejson.ToObject<S7ControlCommand>(); }
            catch (Exception ex) { LogHelper.ErrorLogWrite("S7Plugin", "HandleDeviceControlAsync", ex.ToString(), "S7插件"); return; }
            if (command == null || command.ConContent.IsZxxNullOrEmpty())
            {
                LogHelper.SysLogWrite("S7Plugin", "HandleDeviceControlAsync", $"{PluginName}：设备控制消息格式无效。", "S7插件");
                return;
            }
            if (!string.Equals(command.ClassName?.Trim(), "nets7write", StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.SysLogWrite("S7Plugin", "HandleDeviceControlAsync", $"{PluginName}：不支持的控制类型[{command.ClassName}]，CommandId={command.CommandId}。", "S7插件");
                return;
            }
            NetS7Write? model;
            try { model = command.ConContent.ToObject<NetS7Write>(); }
            catch { LogHelper.SysLogWrite("S7Plugin", "HandleDeviceControlAsync", $"{PluginName}：NetS7Write解析失败。", "S7插件"); return; }
            if (model == null || model.ParamCode.IsZxxNullOrEmpty()) return;

            foreach (var deviceid in command.DeviceIds.Distinct())
            {
                S7DeviceBinding? binding;
                lock (_bindingLock) { _deviceMap.TryGetValue(deviceid, out binding); }
                if (binding == null)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, "", false, "未找到设备绑定信息");
                    continue;
                }
                var point = binding.Points.Find(t => string.Equals(t.ParamCode, model.ParamCode, StringComparison.OrdinalIgnoreCase));
                if (point == null || !point.CollectWritable)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, binding.Device.DeviceName, false, "点位不存在或不可写");
                    continue;
                }
                if (!binding.Online)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, binding.Device.DeviceName, false, "PLC离线，写请求未受理");
                    continue;
                }
                binding.WriteQueue.Enqueue(new S7WriteRequest
                {
                    CommandId = command.CommandId,
                    Point = point,
                    ParamValue = model.ParamValue
                });
                LogHelper.SysLogWrite("S7Plugin", "HandleDeviceControlAsync", $"{PluginName}：写点位入队，设备[{deviceid}]参数[{model.ParamCode}]值[{model.ParamValue}]。", "S7插件");
            }
        }

        #endregion
    }
}
