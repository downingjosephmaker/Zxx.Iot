using System.Globalization;
using CenBoCommon.Zxx;
using CenboEventBus;
using IotDriverCore;
using IotLog;
using IotModel;
using NewLife;
using NewLife.Threading;

namespace IotPlugin.Modbus
{
    /// <summary>
    /// Modbus通用采集控制插件(M3.2:基于IotDriverCore驱动框架的首个协议插件;
    /// 双通道模式——设备DevicePort&gt;0为TCP客户端拨出(MBAP帧),DevicePort=0为RTU over TCP
    /// 服务端等DTU拨入(按DeviceIp归一端点);点表寻址取DeviceTypeParam的collect_*列+ParamAddr,
    /// 从站号取DeviceInfo.DeviceAdr,倍率经ParamFormula公式引擎;DTU注册包路由待M3.5透传接入)
    /// </summary>
    public class ModbusPlugin : ICenBoPlugin
    {
        private IEventBus<PluginEvent>? _eventBus;
        private ModbusPluginConfig? _config;
        private TimerX? _heartTimer;

        /// <summary>RTU over TCP服务端通道(DTU拨入)</summary>
        private TcpServerChannel? _serverChannel;

        /// <summary>Modbus TCP客户端通道池(平台拨出)</summary>
        private TcpClientChannelPool? _clientPool;

        private ChannelCommandEngine? _serverEngine;
        private ChannelCommandEngine? _clientEngine;

        /// <summary>MBAP拆帧定界器(仅TCP客户端路径;RTU over TCP无长度域不可靠拆帧,
        /// 服务端依赖DTU按串口空闲间隙切包保持一包一帧)</summary>
        private readonly FrameAccumulator _mbapAccumulator = new(FrameAccumulator.ExtractMbap);

        private readonly object _bindingLock = new();

        /// <summary>设备ID→绑定</summary>
        private Dictionary<int, ModbusDeviceBinding> _deviceMap = new();

        /// <summary>端点→绑定清单(RTU一条485总线可挂多从站)</summary>
        private Dictionary<string, List<ModbusDeviceBinding>> _endpointMap = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>MBAP事务号发号器</summary>
        private int _transactionId;

        /// <summary>
        /// Modbus设备绑定(设备+实时参数模板+点表+通道模式)
        /// </summary>
        private class ModbusDeviceBinding
        {
            public DeviceInfo Device = null!;
            public DeviceParamEntity? DeviceParam;
            public List<DeviceTypeParam> Points = new();
            public bool IsTcpMode;
            public string Endpoint = "";
        }

        public void PluginInit(IEventBus<PluginEvent> eventBus) => _eventBus = eventBus;

        public string PluginName => "ModbusPlugin";
        public string PluginType => "Modbus通用采集插件";
        public string PluginDesc => "Modbus RTU/TCP通用采集与控制(基于IotDriverCore驱动框架)";
        public string PluginVersion => "1.0.0";
        public string PluginGuid => "b8c4d0e6f2a3517b4cd85e6f9012b345";
        public string PluginModelPath => "";

        /// <summary>
        /// 插件自描述清单(B-1.3:配置schema+缺省配置+控制命令清单+寻址说明,
        /// 宿主在上传/加载时持久化到sys_plugin.plugin_manifest)
        /// </summary>
        public string PluginManifest => PluginMetaBuilder.BuildManifest(
            ModbusPluginConfig.Current,
            new[]
            {
                new PluginMetaBuilder.PluginCommandMeta("netmodbuswrite", "Modbus写点位(FC03保持寄存器经FC06/16下发,FC01线圈经FC05下发)")
            },
            "点表寻址:DeviceTypeParam.collect_*列+ParamAddr,从站号=DeviceInfo.DeviceAdr;DevicePort>0=TCP拨出(MBAP),DevicePort=0=RTU over TCP拨入(DTU)");

        #region 启动/停止

        /// <summary>
        /// 解析插件配置(B-1.1配置单轨化:DB plugin_config为唯一事实源;
        /// 传入为空时回落本地Config文件仅作首次迁移来源,JSON解析失败返回null由启动失败兜底)
        /// </summary>
        private ModbusPluginConfig? LoadConfig(string _PluginConfig)
        {
            if (_PluginConfig.IsZxxNullOrEmpty()) return ModbusPluginConfig.Current;
            try
            {
                return _PluginConfig.ToObject<ModbusPluginConfig>();
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return null;
            }
        }

        /// <summary>
        /// 启动插件:校验配置→加载设备绑定与点表→按模式拉起服务端/客户端通道与指令引擎→
        /// 入队循环采集指令→启动心跳
        /// </summary>
        public async Task<bool> PluginStart(string _PluginConfig)
        {
            try
            {
                _config = LoadConfig(_PluginConfig);
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
                if (_config.SendIntervalMs <= 0 || _config.CmdTimeoutSeconds <= 0 || _config.CollectCycleMs <= 0)
                {
                    LogHelper.Info($"{PluginName}：配置参数存在非正数，插件启动失败。");
                    return false;
                }
                if (!RefreshBindings(out var err))
                {
                    LogHelper.Info($"{PluginName}：初始化设备失败，{err}");
                    return false;
                }

                bool hasserver, hastcp;
                lock (_bindingLock)
                {
                    hasserver = _endpointMap.Values.Any(list => list.Any(b => !b.IsTcpMode));
                    hastcp = _endpointMap.Values.Any(list => list.Any(b => b.IsTcpMode));
                }

                // RTU over TCP服务端(DTU拨入,来源IP归一到DeviceIp端点键)
                if (hasserver)
                {
                    if (_config.NetPort <= 0)
                    {
                        LogHelper.Info($"{PluginName}：存在RTU拨入设备但NetPort未配置，相关设备不采集。");
                    }
                    else
                    {
                        _serverChannel = new TcpServerChannel(_config.NetPort)
                        {
                            EndpointResolver = ResolveServerEndpoint,
                            // DTU注册包模式(§6.6:配置启用后拨入连接先发ASCII注册ID匹配DeviceGateway)
                            RegistrationResolver = _config.EnableDtuRegistration ? new DtuRegistrationHandler(ResolveDtuRegistration) : null,
                            // RTU over TCP无长度域不可靠拆帧,依赖DTU按串口空闲间隙切包(一包一帧)
                            FrameReceived = (ep, frame) => OnFrame(_serverEngine, ep, frame, false),
                            SessionOpened = ep => PublishRunStateByEndpoint(ep, 2),
                            SessionClosed = ep => PublishRunStateByEndpoint(ep, 0)
                        };
                        _serverEngine = new ChannelCommandEngine(_serverChannel, _config.SendIntervalMs)
                        {
                            TimeoutHandler = OnCommandTimeout
                        };
                        if (!_serverChannel.Start()) return false;
                    }
                }

                // Modbus TCP客户端(平台拨出到DeviceIp:DevicePort)
                if (hastcp)
                {
                    _clientPool = new TcpClientChannelPool
                    {
                        // MBAP按长度域拆帧重组(§6.4:粘包拆多帧/半包留缓冲等待)
                        FrameReceived = (ep, data) =>
                        {
                            foreach (var frame in _mbapAccumulator.Push(ep, data))
                            {
                                OnFrame(_clientEngine, ep, frame, true);
                            }
                        },
                        SessionOpened = ep => PublishRunStateByEndpoint(ep, 2),
                        SessionClosed = ep =>
                        {
                            _mbapAccumulator.Reset(ep);  //断连清缓冲,防旧数据串入新连接
                            PublishRunStateByEndpoint(ep, 0);
                        }
                    };
                    lock (_bindingLock)
                    {
                        foreach (var kv in _endpointMap.Where(t => t.Value[0].IsTcpMode))
                        {
                            var device = kv.Value[0].Device;
                            _clientPool.AddEndpoint(kv.Key, device.DeviceIp, device.DevicePort);
                        }
                    }
                    _clientEngine = new ChannelCommandEngine(_clientPool, _config.SendIntervalMs)
                    {
                        TimeoutHandler = OnCommandTimeout
                    };
                }

                RebuildCollectQueues();
                _serverEngine?.Start();
                _clientEngine?.Start();
                _clientPool?.Start();

                _heartTimer?.Dispose();
                _heartTimer = new TimerX(HeartBeatDown, null, 5_000, _config.HeartSecond * 1_000);
                LogHelper.Info($"{PluginName}：插件启动成功。");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// 停止插件:销毁心跳→停引擎→释放通道→清空绑定
        /// </summary>
        public async Task<bool> PluginStop()
        {
            try
            {
                _heartTimer?.Dispose();
                if (_serverEngine != null)
                {
                    await _serverEngine.StopAsync();
                    _serverEngine.ClearAll();
                    _serverEngine = null;
                }
                if (_clientEngine != null)
                {
                    await _clientEngine.StopAsync();
                    _clientEngine.ClearAll();
                    _clientEngine = null;
                }
                _serverChannel?.Dispose();
                _serverChannel = null;
                _clientPool?.Dispose();
                _clientPool = null;
                lock (_bindingLock)
                {
                    _deviceMap = new Dictionary<int, ModbusDeviceBinding>();
                    _endpointMap = new Dictionary<string, List<ModbusDeviceBinding>>(StringComparer.OrdinalIgnoreCase);
                }
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
        /// 从数据库加载设备绑定与点表并原子替换内存映射
        /// (点表条件:类型编码归属本插件且collect_func_code为1~4)
        /// </summary>
        private bool RefreshBindings(out string error)
        {
            error = "";
            var typecodes = _config!.DeviceTypeCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
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

            var devicemap = new Dictionary<int, ModbusDeviceBinding>();
            var endpointmap = new Dictionary<string, List<ModbusDeviceBinding>>(StringComparer.OrdinalIgnoreCase);
            foreach (var device in devices)
            {
                if (device.DeviceIp.IsZxxNullOrEmpty())
                {
                    LogHelper.Info($"{PluginName}：设备[{device.DeviceId}({device.DeviceName})]未配置IP，跳过。");
                    continue;
                }
                if (!pointmap.TryGetValue(device.DeviceTypeCode, out var points) || !points.IsZxxAny())
                {
                    LogHelper.Info($"{PluginName}：类型[{device.DeviceTypeCode}]无Modbus点表配置，设备[{device.DeviceId}]跳过。");
                    continue;
                }
                bool tcpmode = device.DevicePort > 0;
                var binding = new ModbusDeviceBinding
                {
                    Device = device,
                    DeviceParam = devparams?.Find(t => t.DeviceId == device.DeviceId),
                    Points = points,
                    IsTcpMode = tcpmode,
                    Endpoint = tcpmode ? $"{device.DeviceIp}:{device.DevicePort}" : device.DeviceIp
                };
                devicemap[device.DeviceId] = binding;
                if (!endpointmap.TryGetValue(binding.Endpoint, out var list))
                {
                    list = new List<ModbusDeviceBinding>();
                    endpointmap[binding.Endpoint] = list;
                }
                list.Add(binding);
            }
            if (!devicemap.Any())
            {
                error = "未构建任何有效设备绑定。";
                return false;
            }

            lock (_bindingLock)
            {
                _deviceMap = devicemap;
                _endpointMap = endpointmap;
            }
            LogHelper.Info($"{PluginName}：设备映射初始化完成，{devicemap.Count}台设备，{endpointmap.Count}个端点。");
            return true;
        }

        /// <summary>
        /// 按端点重建循环采集指令队列(同端点多设备的批次合并入同一队列串行发送)
        /// </summary>
        private void RebuildCollectQueues()
        {
            Dictionary<string, List<ModbusDeviceBinding>> snapshot;
            lock (_bindingLock) { snapshot = _endpointMap.ToDictionary(t => t.Key, t => t.Value.ToList(), StringComparer.OrdinalIgnoreCase); }
            foreach (var kv in snapshot)
            {
                var engine = kv.Value[0].IsTcpMode ? _clientEngine : _serverEngine;
                if (engine == null) continue;
                var cmds = kv.Value.SelectMany(BuildCollectCommands).ToList();
                if (cmds.IsZxxAny()) engine.ReplaceQueue(kv.Key, cmds);
            }
        }

        /// <summary>
        /// 为单台设备生成合包后的循环采集指令(FC01/02按位1点1位,FC03/04按数据类型推导寄存器数)
        /// </summary>
        private List<DriverCommand> BuildCollectCommands(ModbusDeviceBinding binding)
        {
            var driverpoints = binding.Points.Select(t => new DriverPoint
            {
                DeviceId = binding.Device.DeviceId,
                ParamCode = t.ParamCode,
                SlaveAddr = binding.Device.DeviceAdr,
                FuncCode = (byte)t.CollectFuncCode,
                Address = t.ParamAddr,
                Length = t.CollectFuncCode <= 2 ? 1 : ModbusValueCodec.InferRegLength(t.CollectDataType, t.CollectRegLength),
                DataType = t.CollectDataType ?? "",
                ByteOrder = t.CollectByteOrder ?? "",
                BitOffset = t.CollectBitOffset
            }).ToList();

            var result = new List<DriverCommand>();
            foreach (var batch in PointBatchBuilder.Build(driverpoints, _config!.MaxBatchLength, _config.GapTolerance))
            {
                byte slave = (byte)batch.SlaveAddr;
                byte func = batch.FuncCode;
                var payload = binding.IsTcpMode
                    ? ModbusFrameHelper.BuildReadTcp(NextTid(), slave, func, (ushort)batch.StartAddress, (ushort)batch.TotalLength)
                    : ModbusFrameHelper.BuildReadRtu(slave, func, (ushort)batch.StartAddress, (ushort)batch.TotalLength);
                result.Add(new DriverCommand
                {
                    CmdKind = DriverCommand.KindCollect,
                    Endpoint = binding.Endpoint,
                    DeviceId = binding.Device.DeviceId,
                    DeviceAddr = binding.Device.DeviceAdr,
                    Payload = payload,
                    ResponseMatcher = binding.IsTcpMode ? BuildTcpMatcher(slave, func) : BuildRtuMatcher(slave, func),
                    TimeoutSeconds = _config.CmdTimeoutSeconds,
                    CycleMs = _config.CollectCycleMs,
                    Tag = batch
                });
            }
            return result;
        }

        /// <summary>
        /// RTU应答匹配器(从站地址+功能码或其异常码)
        /// </summary>
        private static Func<byte[], bool> BuildRtuMatcher(byte slave, byte func) =>
            frame => frame.Length >= 2 && frame[0] == slave && (frame[1] == func || frame[1] == (byte)(func | 0x80));

        /// <summary>
        /// TCP应答匹配器(单元标识+功能码或其异常码)
        /// </summary>
        private static Func<byte[], bool> BuildTcpMatcher(byte unit, byte func) =>
            frame => ModbusFrameHelper.TryParseTcp(frame, out _, out var u, out var f, out _)
                     && u == unit && (f == func || f == (byte)(func | 0x80));

        /// <summary>
        /// 取下一个MBAP事务号
        /// </summary>
        private ushort NextTid() => (ushort)Interlocked.Increment(ref _transactionId);

        /// <summary>
        /// 拨入来源IP→绑定端点键(RTU模式端点键即DeviceIp,未命中返回null回退"IP:Port")
        /// </summary>
        private string? ResolveServerEndpoint(string ip)
        {
            lock (_bindingLock)
            {
                return _endpointMap.Keys.FirstOrDefault(k => k.Equals(ip, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// DTU注册包→绑定端点键(§6.6:取可打印ASCII前缀作注册ID,匹配拨入设备的网关编号DeviceGateway;
        /// 未匹配返回null继续等待,超时由通道踢连接;紧随的回车换行一并消费)
        /// </summary>
        private string? ResolveDtuRegistration(byte[] data, out int consumed)
        {
            consumed = 0;
            int end = 0;
            while (end < data.Length && end < 64 && data[end] >= 0x20 && data[end] <= 0x7E) end++;
            if (end == 0) return null;
            string regid = System.Text.Encoding.ASCII.GetString(data, 0, end).Trim();
            if (regid.Length == 0) return null;
            string? endpoint = null;
            lock (_bindingLock)
            {
                foreach (var kv in _endpointMap)
                {
                    if (kv.Value.Any(b => !b.IsTcpMode && regid.Equals(b.Device.DeviceGateway?.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        endpoint = kv.Key;
                        break;
                    }
                }
            }
            if (endpoint == null)
            {
                LogHelper.Info($"{PluginName}：DTU注册ID[{regid}]未匹配任何设备网关编号，等待注册超时。");
                return null;
            }
            consumed = end;
            while (consumed < data.Length && (data[consumed] == 0x0D || data[consumed] == 0x0A)) consumed++;
            LogHelper.Info($"{PluginName}：DTU注册[{regid}]绑定端点[{endpoint}]。");
            return endpoint;
        }

        #endregion

        #region 收帧处理

        /// <summary>
        /// 入站帧核心处理:帧校验→回执匹配→异常码/控制回执/采集数据三路分发
        /// </summary>
        private void OnFrame(ChannelCommandEngine? engine, string endpoint, byte[] buffer, bool tcpmode)
        {
            try
            {
                if (engine == null) return;
                byte slave;
                byte func;
                byte[] data;
                if (tcpmode)
                {
                    if (!ModbusFrameHelper.TryParseTcp(buffer, out _, out slave, out func, out data))
                    {
                        LogHelper.Info($"{PluginName}：MBAP帧解析失败，来自{endpoint}，{buffer.ToHex()}");
                        return;
                    }
                }
                else
                {
                    if (!ModbusFrameHelper.TryParseRtu(buffer, out slave, out func, out data))
                    {
                        LogHelper.Info($"{PluginName}：CRC校验失败，来自{endpoint}，{buffer.ToHex()}");
                        return;
                    }
                }

                var cmd = engine.MatchResponse(endpoint, buffer);
                if (cmd == null)
                {
                    LogHelper.Info($"{PluginName}：未匹配到等待指令，来自{endpoint}，{buffer.ToHex()}");
                    return;
                }

                // 异常应答(功能码|0x80,数据域首字节为异常码)
                if ((func & 0x80) != 0)
                {
                    byte errcode = data.Length > 0 ? data[0] : (byte)0;
                    LogHelper.Info($"{PluginName}：设备[{cmd.DeviceId}]返回异常码{errcode}，功能码{func & 0x7F}。");
                    if (cmd.CmdKind == DriverCommand.KindControl)
                    {
                        PublishControlResult(cmd, false, $"设备返回异常码{errcode}");
                        engine.Remove(cmd);
                    }
                    return;
                }

                // 控制回执(FC06/16应答为地址回显,匹配即成功)
                if (cmd.CmdKind == DriverCommand.KindControl)
                {
                    LogHelper.Info($"{PluginName}：写控制回执成功，设备[{cmd.DeviceId}]。");
                    PublishControlResult(cmd, true, "控制成功");
                    engine.Remove(cmd);
                    engine.AccelerateCollect(endpoint, cmd.DeviceAddr);
                    return;
                }

                // 采集应答:data=[字节数,寄存器/线圈数据...]
                if (cmd.Tag is not PointBatch batch || data.Length < 1) return;
                int bytecount = data[0];
                if (data.Length < bytecount + 1) return;
                var payload = new byte[bytecount];
                Array.Copy(data, 1, payload, 0, bytecount);
                _ = Task.Run(() => PublishBatchData(batch, payload));
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 控制指令最终超时回调
        /// </summary>
        private void OnCommandTimeout(DriverCommand cmd) => PublishControlResult(cmd, false, "指令超时");

        #endregion

        #region 数据解码与发布

        /// <summary>
        /// 解码一个批次的应答数据并发布协议解析消息(批次内点位同属一台设备)
        /// </summary>
        private void PublishBatchData(PointBatch batch, byte[] payload)
        {
            try
            {
                if (!batch.Points.IsZxxAny()) return;
                ModbusDeviceBinding? binding;
                lock (_bindingLock) { _deviceMap.TryGetValue(batch.Points[0].DeviceId, out binding); }
                if (binding == null) return;

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var point in batch.Points)
                {
                    var value = DecodePoint(batch, point, payload);
                    if (value != null) values[point.ParamCode] = value;
                }
                if (!values.Any()) return;

                var data = BuildDeviceData(binding, values);
                if (data == null) return;
                _ = SendMessageAsync(new PluginMessage
                {
                    MessageType = PluginMessageEnum.协议解析,
                    MessageJson = new List<DeviceData> { data }.ToJson()
                });
            }
            catch (Exception ex) { LogHelper.Error(ex); }
        }

        /// <summary>
        /// 从批次应答中切片解码单点(FC01/02按位取值,FC03/04按寄存器偏移切片)
        /// </summary>
        private static string? DecodePoint(PointBatch batch, DriverPoint point, byte[] payload)
        {
            if (batch.FuncCode <= 2)
            {
                int bitindex = point.Address - batch.StartAddress;
                int byteindex = bitindex / 8;
                if (bitindex < 0 || byteindex >= payload.Length) return null;
                return ((payload[byteindex] >> (bitindex % 8)) & 1).ToString();
            }
            int offset = (point.Address - batch.StartAddress) * 2;
            int length = Math.Max(1, point.Length) * 2;
            if (offset < 0 || offset + length > payload.Length) return null;
            var slice = new byte[length];
            Array.Copy(payload, offset, slice, 0, length);
            return ModbusValueCodec.Decode(slice, point.DataType, point.ByteOrder, point.BitOffset);
        }

        /// <summary>
        /// 按设备参数模板构建DeviceData(套用ParamFormula公式,按最大/最小合法值标记IsAlarm)
        /// </summary>
        private DeviceData? BuildDeviceData(ModbusDeviceBinding binding, Dictionary<string, string> values)
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
        /// 发布端点下所有设备的运行状态(2在线/0离线,连接建立与断开时触发)
        /// </summary>
        private void PublishRunStateByEndpoint(string endpoint, int state)
        {
            try
            {
                List<ModbusDeviceBinding> bindings;
                lock (_bindingLock)
                {
                    if (!_endpointMap.TryGetValue(endpoint, out var list) || !list.IsZxxAny()) return;
                    bindings = list.ToList();
                }
                var datalist = new List<DeviceData>();
                foreach (var binding in bindings)
                {
                    var dev = new DeviceInfoEntity();
                    binding.Device.CopyTypeValue(dev);
                    dev.DeviceState = state;
                    datalist.Add(new DeviceData
                    {
                        DeviceId = dev.DeviceId,
                        device = dev,
                        deviceparam = new List<Expand_DeviceParam>(),
                        paramtype = 0
                    });
                }
                if (!datalist.IsZxxAny()) return;
                _ = SendMessageAsync(new PluginMessage
                {
                    MessageType = PluginMessageEnum.运行状态,
                    MessageJson = datalist.ToJson()
                });
                LogHelper.Info($"{PluginName}：{endpoint}{(state == 2 ? "上线" : "离线")}，涉及{datalist.Count}台设备。");
            }
            catch (Exception ex) { LogHelper.Error(ex); }
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
                    LogHelper.Info($"{PluginName}：收到心跳。");
                    break;
                case PluginMessageEnum.设备控制:
                    await HandleDeviceControlAsync(mess.MessageJson);
                    break;
            }
        }

        /// <summary>
        /// 处理写点位控制(netmodbuswrite:按参数编码定位点表,FC03保持寄存器经FC06/16下发,
        /// 写优先由指令引擎的控制优先调度保证)
        /// </summary>
        private async Task HandleDeviceControlAsync(string? messagejson)
        {
            if (messagejson.IsZxxNullOrEmpty())
            {
                LogHelper.Info($"{PluginName}：设备控制消息为空。");
                return;
            }
            ModbusControlCommand? command;
            try { command = messagejson.ToObject<ModbusControlCommand>(); }
            catch (Exception ex) { LogHelper.Error(ex); return; }
            if (command == null || command.ConContent.IsZxxNullOrEmpty())
            {
                LogHelper.Info($"{PluginName}：设备控制消息格式无效。");
                return;
            }
            if (!string.Equals(command.ClassName?.Trim(), "netmodbuswrite", StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.Info($"{PluginName}：不支持的控制类型[{command.ClassName}]，CommandId={command.CommandId}。");
                return;
            }
            NetModbusWrite? model;
            try { model = command.ConContent.ToObject<NetModbusWrite>(); }
            catch { LogHelper.Info($"{PluginName}：NetModbusWrite解析失败。"); return; }
            if (model == null || model.ParamCode.IsZxxNullOrEmpty()) return;

            foreach (var deviceid in command.DeviceIds.Distinct())
            {
                ModbusDeviceBinding? binding;
                lock (_bindingLock) { _deviceMap.TryGetValue(deviceid, out binding); }
                if (binding == null)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, "", false, "未找到设备绑定信息");
                    continue;
                }
                var point = binding.Points.Find(t => string.Equals(t.ParamCode, model.ParamCode, StringComparison.OrdinalIgnoreCase));
                if (point == null || !point.CollectWritable || point.CollectFuncCode != 3)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, binding.Device.DeviceName, false, "点位不可写或非保持寄存器");
                    continue;
                }
                int reglen = ModbusValueCodec.InferRegLength(point.CollectDataType, point.CollectRegLength);
                var registerbytes = ModbusValueCodec.Encode(model.ParamValue, point.CollectDataType, point.CollectByteOrder, reglen);
                if (registerbytes == null)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, binding.Device.DeviceName, false, "值编码失败或类型不支持写入");
                    continue;
                }

                byte slave = (byte)binding.Device.DeviceAdr;
                ushort addr = (ushort)point.ParamAddr;
                byte expectfunc = registerbytes.Length <= 2 ? (byte)0x06 : (byte)0x10;
                byte[] payload;
                if (binding.IsTcpMode)
                {
                    payload = registerbytes.Length <= 2
                        ? ModbusFrameHelper.BuildWriteSingleTcp(NextTid(), slave, addr, (ushort)((registerbytes[0] << 8) | registerbytes[1]))
                        : ModbusFrameHelper.BuildWriteMultiTcp(NextTid(), slave, addr, registerbytes);
                }
                else
                {
                    payload = registerbytes.Length <= 2
                        ? ModbusFrameHelper.BuildWriteSingleRtu(slave, addr, (ushort)((registerbytes[0] << 8) | registerbytes[1]))
                        : ModbusFrameHelper.BuildWriteMultiRtu(slave, addr, registerbytes);
                }
                var engine = binding.IsTcpMode ? _clientEngine : _serverEngine;
                if (engine == null)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, binding.Device.DeviceName, false, "通道未启用");
                    continue;
                }
                engine.Enqueue(new DriverCommand
                {
                    CmdKind = DriverCommand.KindControl,
                    Endpoint = binding.Endpoint,
                    DeviceId = deviceid,
                    DeviceAddr = binding.Device.DeviceAdr,
                    Payload = payload,
                    ResponseMatcher = binding.IsTcpMode ? BuildTcpMatcher(slave, expectfunc) : BuildRtuMatcher(slave, expectfunc),
                    WaitForResponse = true,
                    OneShot = true,
                    TimeoutSeconds = _config?.CmdTimeoutSeconds ?? 10,
                    RetryLimit = _config?.RetryLimit ?? 1,
                    CommandId = command.CommandId,
                    ClassName = "NetModbusWrite"
                });
                LogHelper.Info($"{PluginName}：写点位入队，设备[{deviceid}]参数[{model.ParamCode}]值[{model.ParamValue}]。");
            }
        }

        #endregion

        #region 控制结果发布

        /// <summary>
        /// 按指令回执发布控制结果
        /// </summary>
        private void PublishControlResult(DriverCommand cmd, bool success, string message)
        {
            if (cmd.CommandId.IsZxxNullOrEmpty()) return;
            string devicename;
            lock (_bindingLock)
            {
                devicename = _deviceMap.TryGetValue(cmd.DeviceId, out var binding) ? binding.Device.DeviceName : "";
            }
            _ = PublishControlResultAsync(cmd.CommandId, cmd.DeviceId, devicename, success, message);
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
    }
}
