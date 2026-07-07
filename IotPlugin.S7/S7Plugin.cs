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
    /// 按(区,DB)分组经PointBatchBuilder连续性合包批量读;暂只读,写下发待后续)
    /// </summary>
    public class S7Plugin : ICenBoPlugin
    {
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
        }

        public void PluginInit(IEventBus<PluginEvent> eventBus) => _eventBus = eventBus;

        public string PluginName => "S7Plugin";
        public string PluginType => "西门子S7采集插件";
        public string PluginDesc => "西门子S7系列PLC批量采集(基于IotDriverCore驱动框架,S7netplus)";
        public string PluginVersion => "1.0.0";
        public string PluginGuid => "e1f7a3b9c5d6840e7fa18b9c2345e678";
        public string PluginModelPath => "";

        #region 启动/停止

        /// <summary>
        /// 启动插件:校验配置→加载绑定与点表→每设备拉起独立采集循环→启动心跳
        /// </summary>
        public async Task<bool> PluginStart(string _PluginConfig)
        {
            try
            {
                _config = S7PluginConfig.Current;
                if (_config == null)
                {
                    LogHelper.Info($"{PluginName}：配置加载失败，插件启动失败。");
                    return false;
                }
                if (_config.DeviceTypeCodes.IsZxxNullOrEmpty())
                {
                    LogHelper.Info($"{PluginName}：未配置设备类型编码，插件不启用。");
                    return false;
                }
                if (_config.CollectCycleMs <= 0 || _config.MaxBatchBytes <= 0)
                {
                    LogHelper.Info($"{PluginName}：配置参数存在非正数，插件启动失败。");
                    return false;
                }
                if (!RefreshBindings(out var err))
                {
                    LogHelper.Info($"{PluginName}：初始化设备失败，{err}");
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
                LogHelper.Info($"{PluginName}：插件启动成功，{bindings.Count}台PLC。");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
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
            }
            catch (Exception ex) { LogHelper.Error(ex); }
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
            catch (Exception ex) { LogHelper.Error(ex); }
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
                    LogHelper.Info($"{PluginName}：设备[{device.DeviceId}({device.DeviceName})]未配置IP，跳过。");
                    continue;
                }
                if (!pointmap.TryGetValue(device.DeviceTypeCode, out var points) || !points.IsZxxAny())
                {
                    LogHelper.Info($"{PluginName}：类型[{device.DeviceTypeCode}]无S7点表配置，设备[{device.DeviceId}]跳过。");
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
            LogHelper.Info($"{PluginName}：PLC映射初始化完成，{devicemap.Count}台。");
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
                LogHelper.Info($"{PluginName}：设备[{binding.Device.DeviceId}]无有效采集批次。");
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
                    LogHelper.Info($"{PluginName}：PLC[{binding.Device.DeviceId}({binding.Device.DeviceIp})]连接建立。");
                    PublishRunState(binding, 2);

                    while (!token.IsCancellationRequested && plc.IsConnected)
                    {
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
                    LogHelper.Info($"{PluginName}：PLC[{binding.Device.DeviceId}]通信异常，{ex.Message}");
                }
                finally
                {
                    if (binding.Online)
                    {
                        binding.Online = false;
                        LogHelper.Info($"{PluginName}：PLC[{binding.Device.DeviceId}]连接断开。");
                        PublishRunState(binding, 0);
                    }
                    try { plc?.Close(); } catch { }
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
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        #endregion

        #region 接收主程序消息

        /// <summary>
        /// 接收主程序消息入口(心跳;S7写下发待后续实现)
        /// </summary>
        public async Task ReceiveMessageAsync(PluginMessage mess)
        {
            switch (mess.MessageType)
            {
                case PluginMessageEnum.心跳:
                    LogHelper.Info($"{PluginName}：收到心跳。");
                    break;
                case PluginMessageEnum.设备控制:
                    LogHelper.Info($"{PluginName}：S7写下发暂未实现，忽略控制消息。");
                    break;
            }
        }

        #endregion
    }
}
