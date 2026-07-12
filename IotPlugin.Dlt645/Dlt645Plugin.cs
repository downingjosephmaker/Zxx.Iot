using System.Globalization;
using CenBoCommon.Zxx;
using CenboEventBus;
using IotDriverCore;
using IotLog;
using IotModel;
using NewLife;
using NewLife.Threading;

namespace IotPlugin.Dlt645
{
    /// <summary>
    /// DL/T 645电表采集插件(M3.3:2007版4字节DI/1997版2字节DI按类型编码配置;
    /// 一表一DI一问一答总线串行,应答按帧内6字节表地址路由子设备(一条DTU挂多表);
    /// 表地址从DeviceAdr十进制左补零到12位BCD派生;点表DI取ParamAddr,
    /// CollectFuncCode>0即启用采集,值为BCD原始数字串经ParamFormula定标;
    /// 广播校时经设备控制消息netdlt645timesync触发,平台定时任务待接)
    /// </summary>
    public class Dlt645Plugin : ICenBoPlugin, ISimulatable
    {
        // ===== 模拟人格(方案A:独立端口/生命周期,委托Dlt645Simulator) =====
        private readonly Sim.Dlt645Simulator _simulator = new();

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

        private IEventBus<PluginEvent>? _eventBus;
        private Dlt645PluginConfig? _config;
        private TimerX? _heartTimer;

        /// <summary>DTU透传服务端通道(拨入)</summary>
        private TcpServerChannel? _serverChannel;

        /// <summary>TCP客户端通道池(拨出)</summary>
        private TcpClientChannelPool? _clientPool;

        private ChannelCommandEngine? _serverEngine;
        private ChannelCommandEngine? _clientEngine;

        /// <summary>645拆帧定界器(双68定界+长度域,服务端/客户端两路共用,断连清缓冲)</summary>
        private readonly FrameAccumulator _accumulator = new(FrameAccumulator.ExtractDlt645);

        private readonly object _bindingLock = new();

        /// <summary>设备ID→绑定</summary>
        private Dictionary<int, Dlt645DeviceBinding> _deviceMap = new();

        /// <summary>端点→绑定清单(一条DTU总线可挂多表)</summary>
        private Dictionary<string, List<Dlt645DeviceBinding>> _endpointMap = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 645电表绑定(设备+实时参数模板+点表+协议版本+通道模式)
        /// </summary>
        private class Dlt645DeviceBinding
        {
            public DeviceInfo Device = null!;
            public DeviceParamEntity? DeviceParam;
            public List<DeviceTypeParam> Points = new();
            public bool Is1997;
            public bool IsTcpMode;
            public string Endpoint = "";
            public byte[] AddressBcd = Array.Empty<byte>();
        }

        public void PluginInit(IEventBus<PluginEvent> eventBus) => _eventBus = eventBus;

        public string PluginName => "Dlt645Plugin";
        public string PluginType => "DLT645电表采集插件";
        public string PluginDesc => "DL/T 645-2007/1997电表抄读与广播校时(基于IotDriverCore驱动框架)";
        public string PluginVersion => "1.0.0";
        public string PluginGuid => "c9d5e1f7a3b4628c5de96f7a0123c456";
        public string PluginModelPath => "";

        /// <summary>
        /// 插件自描述清单(B-1.3:配置schema+缺省配置+控制命令清单+寻址说明,
        /// 宿主在上传/加载时持久化到sys_plugin.plugin_manifest)
        /// </summary>
        public string PluginManifest => PluginMetaBuilder.BuildManifest(
            Dlt645PluginConfig.Current,
            new[]
            {
                new PluginMetaBuilder.PluginCommandMeta("netdlt645timesync", "广播校时(向指令涉及设备所在的全部端点广播,C=08H无应答)"),
                new PluginMetaBuilder.PluginCommandMeta("netdlt645read", "加速抄读(重置目标表抄读指令的下次发送时刻)")
            },
            "点表寻址:ParamAddr=DI标识(2007版4字节/1997版2字节),表地址=DeviceInfo.DeviceAdr十进制左补零到12位BCD;DevicePort=0=DTU透传拨入",
            "dlt645");

        #region 启动/停止

        /// <summary>
        /// 解析插件配置(B-1.1配置单轨化:DB plugin_config为唯一事实源;
        /// 传入为空时回落本地Config文件仅作首次迁移来源,JSON解析失败返回null由启动失败兜底)
        /// </summary>
        private Dlt645PluginConfig? LoadConfig(string _PluginConfig)
        {
            if (_PluginConfig.IsZxxNullOrEmpty()) return Dlt645PluginConfig.Current;
            try
            {
                return _PluginConfig.ToObject<Dlt645PluginConfig>();
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("Dlt645Plugin", "LoadConfig", ex.ToString(), "DLT645插件");
                return null;
            }
        }

        /// <summary>
        /// 启动插件:校验配置→加载绑定与点表→拉起通道与引擎→入队抄读指令→启动心跳
        /// </summary>
        public async Task<bool> PluginStart(string _PluginConfig)
        {
            try
            {
                _config = LoadConfig(_PluginConfig);
                if (_config == null)
                {
                    LogHelper.SysLogWrite("Dlt645Plugin", "PluginStart", $"{PluginName}：配置加载失败，插件启动失败。", "DLT645插件");
                    return false;
                }
                if (_config.DeviceTypeCodes.IsZxxNullOrEmpty())
                {
                    LogHelper.SysLogWrite("Dlt645Plugin", "PluginStart", $"{PluginName}：未配置设备类型编码，插件不启用。", "DLT645插件");
                    return false;
                }
                if (_config.SendIntervalMs <= 0 || _config.CmdTimeoutSeconds <= 0 || _config.CollectCycleMs <= 0)
                {
                    LogHelper.SysLogWrite("Dlt645Plugin", "PluginStart", $"{PluginName}：配置参数存在非正数，插件启动失败。", "DLT645插件");
                    return false;
                }
                if (!RefreshBindings(out var err))
                {
                    LogHelper.SysLogWrite("Dlt645Plugin", "PluginStart", $"{PluginName}：初始化设备失败，{err}", "DLT645插件");
                    return false;
                }

                bool hasserver, hastcp;
                lock (_bindingLock)
                {
                    hasserver = _endpointMap.Values.Any(list => list.Any(b => !b.IsTcpMode));
                    hastcp = _endpointMap.Values.Any(list => list.Any(b => b.IsTcpMode));
                }

                if (hasserver)
                {
                    if (_config.NetPort <= 0)
                    {
                        LogHelper.SysLogWrite("Dlt645Plugin", "PluginStart", $"{PluginName}：存在DTU拨入设备但NetPort未配置，相关设备不采集。", "DLT645插件");
                    }
                    else
                    {
                        _serverChannel = new TcpServerChannel(_config.NetPort)
                        {
                            EndpointResolver = ResolveServerEndpoint,
                            // DTU注册包模式(§6.6:配置启用后拨入连接先发ASCII注册ID匹配DeviceGateway)
                            RegistrationResolver = _config.EnableDtuRegistration ? new DtuRegistrationHandler(ResolveDtuRegistration) : null,
                            // 645按双68定界拆帧重组(§6.4:粘包拆多帧/半包留缓冲等待)
                            FrameReceived = (ep, data) =>
                            {
                                foreach (var frame in _accumulator.Push(ep, data))
                                {
                                    OnFrame(_serverEngine, ep, frame);
                                }
                            },
                            SessionOpened = ep => PublishRunStateByEndpoint(ep, 2),
                            SessionClosed = ep =>
                            {
                                _accumulator.Reset(ep);  //断连清缓冲,防旧数据串入新连接
                                PublishRunStateByEndpoint(ep, 0);
                            }
                        };
                        _serverEngine = new ChannelCommandEngine(_serverChannel, _config.SendIntervalMs)
                        {
                            TimeoutHandler = OnCommandTimeout
                        };
                        if (!_serverChannel.Start()) return false;
                    }
                }

                if (hastcp)
                {
                    _clientPool = new TcpClientChannelPool
                    {
                        FrameReceived = (ep, data) =>
                        {
                            foreach (var frame in _accumulator.Push(ep, data))
                            {
                                OnFrame(_clientEngine, ep, frame);
                            }
                        },
                        SessionOpened = ep => PublishRunStateByEndpoint(ep, 2),
                        SessionClosed = ep =>
                        {
                            _accumulator.Reset(ep);
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
                LogHelper.SysLogWrite("Dlt645Plugin", "PluginStart", $"{PluginName}：插件启动成功。", "DLT645插件");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("Dlt645Plugin", "PluginStart", ex.ToString(), "DLT645插件");
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
                    _deviceMap = new Dictionary<int, Dlt645DeviceBinding>();
                    _endpointMap = new Dictionary<string, List<Dlt645DeviceBinding>>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Dlt645Plugin", "PluginStop", ex.ToString(), "DLT645插件"); }
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
            catch (Exception ex) { LogHelper.ErrorLogWrite("Dlt645Plugin", "SendMessageAsync", ex.ToString(), "DLT645插件"); }
        }

        #endregion

        #region 设备初始化

        /// <summary>
        /// 从数据库加载电表绑定与点表并原子替换内存映射(点表条件:CollectFuncCode>0即启用)
        /// </summary>
        private bool RefreshBindings(out string error)
        {
            error = "";
            var typecodes = _config!.DeviceTypeCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var codes1997 = _config.Dlt1997TypeCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var devices = DeviceInfoDAO.Instance.GetListBy(t => typecodes.Contains(t.DeviceTypeCode) && t.IsCollection == 1);
            if (!devices.IsZxxAny())
            {
                error = "未查询到归属本插件的采集设备。";
                return false;
            }

            var allpoints = DeviceTypeParamDAO.Instance.GetList()?.Cast<DeviceTypeParam>().ToList() ?? new List<DeviceTypeParam>();
            var pointmap = allpoints
                .Where(t => typecodes.Contains(t.DeviceTypeCode) && t.CollectFuncCode > 0)
                .GroupBy(t => t.DeviceTypeCode)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var deviceids = devices.Select(t => t.DeviceId).ToList();
            var devparams = DeviceParamDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));

            var devicemap = new Dictionary<int, Dlt645DeviceBinding>();
            var endpointmap = new Dictionary<string, List<Dlt645DeviceBinding>>(StringComparer.OrdinalIgnoreCase);
            foreach (var device in devices)
            {
                if (device.DeviceIp.IsZxxNullOrEmpty())
                {
                    LogHelper.SysLogWrite("Dlt645Plugin", "RefreshBindings", $"{PluginName}：设备[{device.DeviceId}({device.DeviceName})]未配置IP，跳过。", "DLT645插件");
                    continue;
                }
                if (!pointmap.TryGetValue(device.DeviceTypeCode, out var points) || !points.IsZxxAny())
                {
                    LogHelper.SysLogWrite("Dlt645Plugin", "RefreshBindings", $"{PluginName}：类型[{device.DeviceTypeCode}]无DI点表配置，设备[{device.DeviceId}]跳过。", "DLT645插件");
                    continue;
                }
                bool tcpmode = device.DevicePort > 0;
                var binding = new Dlt645DeviceBinding
                {
                    Device = device,
                    DeviceParam = devparams?.Find(t => t.DeviceId == device.DeviceId),
                    Points = points,
                    Is1997 = codes1997.Contains(device.DeviceTypeCode),
                    IsTcpMode = tcpmode,
                    Endpoint = tcpmode ? $"{device.DeviceIp}:{device.DevicePort}" : device.DeviceIp,
                    AddressBcd = Dlt645FrameHelper.BuildAddressBcd(device.DeviceAdr)
                };
                devicemap[device.DeviceId] = binding;
                if (!endpointmap.TryGetValue(binding.Endpoint, out var list))
                {
                    list = new List<Dlt645DeviceBinding>();
                    endpointmap[binding.Endpoint] = list;
                }
                list.Add(binding);
            }
            if (!devicemap.Any())
            {
                error = "未构建任何有效电表绑定。";
                return false;
            }

            lock (_bindingLock)
            {
                _deviceMap = devicemap;
                _endpointMap = endpointmap;
            }
            LogHelper.SysLogWrite("Dlt645Plugin", "RefreshBindings", $"{PluginName}：电表映射初始化完成，{devicemap.Count}块表，{endpointmap.Count}个端点。", "DLT645插件");
            return true;
        }

        /// <summary>
        /// 按端点重建抄读指令队列(一表一DI一条指令,同总线串行由引擎单飞行保证)
        /// </summary>
        private void RebuildCollectQueues()
        {
            Dictionary<string, List<Dlt645DeviceBinding>> snapshot;
            lock (_bindingLock) { snapshot = _endpointMap.ToDictionary(t => t.Key, t => t.Value.ToList(), StringComparer.OrdinalIgnoreCase); }
            foreach (var kv in snapshot)
            {
                var engine = kv.Value[0].IsTcpMode ? _clientEngine : _serverEngine;
                if (engine == null) continue;
                var cmds = new List<DriverCommand>();
                foreach (var binding in kv.Value)
                {
                    foreach (var point in binding.Points)
                    {
                        uint di = unchecked((uint)point.ParamAddr);
                        cmds.Add(new DriverCommand
                        {
                            CmdKind = DriverCommand.KindCollect,
                            Endpoint = binding.Endpoint,
                            DeviceId = binding.Device.DeviceId,
                            DeviceAddr = binding.Device.DeviceAdr,
                            Payload = Dlt645FrameHelper.BuildReadFrame(binding.AddressBcd, di, binding.Is1997),
                            ResponseMatcher = BuildMatcher(binding),
                            TimeoutSeconds = _config!.CmdTimeoutSeconds,
                            CycleMs = _config.CollectCycleMs,
                            Tag = point
                        });
                    }
                }
                if (cmds.IsZxxAny()) engine.ReplaceQueue(kv.Key, cmds);
            }
        }

        /// <summary>
        /// 应答匹配器(帧内6字节表地址一致且控制码为读应答C|0x80或异常C|0xC0,
        /// 一条DTU挂多表时以帧内地址路由)
        /// </summary>
        private static Func<byte[], bool> BuildMatcher(Dlt645DeviceBinding binding)
        {
            var addr = binding.AddressBcd;
            byte readcode = binding.Is1997 ? Dlt645FrameHelper.ReadCode1997 : Dlt645FrameHelper.ReadCode2007;
            return frame =>
            {
                if (!Dlt645FrameHelper.TryParseFrame(frame, out var a, out var c, out _)) return false;
                if (!a.SequenceEqual(addr)) return false;
                return c == (byte)(readcode | 0x80) || c == (byte)(readcode | 0xC0);
            };
        }

        /// <summary>
        /// 拨入来源IP→绑定端点键(DTU模式端点键即DeviceIp,未命中返回null回退"IP:Port")
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
                LogHelper.SysLogWrite("Dlt645Plugin", "ResolveDtuRegistration", $"{PluginName}：DTU注册ID[{regid}]未匹配任何设备网关编号，等待注册超时。", "DLT645插件");
                return null;
            }
            consumed = end;
            while (consumed < data.Length && (data[consumed] == 0x0D || data[consumed] == 0x0A)) consumed++;
            LogHelper.SysLogWrite("Dlt645Plugin", "ResolveDtuRegistration", $"{PluginName}：DTU注册[{regid}]绑定端点[{endpoint}]。", "DLT645插件");
            return endpoint;
        }

        #endregion

        #region 收帧处理

        /// <summary>
        /// 入站帧核心处理:帧校验→回执匹配→异常码/抄读数据分发
        /// (CS校验失败按方案§6.3丢弃整包,粘包拆帧待M3.5声明式定界器)
        /// </summary>
        private void OnFrame(ChannelCommandEngine? engine, string endpoint, byte[] buffer)
        {
            try
            {
                if (engine == null) return;
                if (!Dlt645FrameHelper.TryParseFrame(buffer, out _, out var code, out var data))
                {
                    LogHelper.SysLogWrite("Dlt645Plugin", "OnFrame", $"{PluginName}：帧校验失败，来自{endpoint}，{buffer.ToHex()}", "DLT645插件");
                    return;
                }

                var cmd = engine.MatchResponse(endpoint, buffer);
                if (cmd == null)
                {
                    LogHelper.SysLogWrite("Dlt645Plugin", "OnFrame", $"{PluginName}：未匹配到等待指令，来自{endpoint}，{buffer.ToHex()}", "DLT645插件");
                    return;
                }

                // 异常应答(C|0xC0,数据域为ERR字节)
                if ((code & 0x40) != 0)
                {
                    byte errcode = data.Length > 0 ? data[0] : (byte)0;
                    LogHelper.SysLogWrite("Dlt645Plugin", "OnFrame", $"{PluginName}：表[{cmd.DeviceId}]返回异常码{errcode}。", "DLT645插件");
                    return;
                }

                if (cmd.Tag is not DeviceTypeParam point) return;
                _ = Task.Run(() => PublishPointData(cmd.DeviceId, point, data));
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Dlt645Plugin", "OnFrame", ex.ToString(), "DLT645插件"); }
        }

        /// <summary>
        /// 控制指令最终超时回调
        /// </summary>
        private void OnCommandTimeout(DriverCommand cmd) => PublishControlResult(cmd, false, "指令超时");

        #endregion

        #region 数据解码与发布

        /// <summary>
        /// 校验应答DI并解码BCD值(值=DI之后的字节,低位在前;
        /// CollectDataType=bcdsigned时最高字节bit7为符号位),按参数模板发布协议解析消息
        /// </summary>
        private void PublishPointData(int deviceid, DeviceTypeParam point, byte[] data)
        {
            try
            {
                Dlt645DeviceBinding? binding;
                lock (_bindingLock) { _deviceMap.TryGetValue(deviceid, out binding); }
                if (binding == null) return;

                int dilen = binding.Is1997 ? 2 : 4;
                if (data.Length <= dilen) return;
                uint di = Dlt645FrameHelper.ReadDi(data, binding.Is1997);
                if (di != unchecked((uint)point.ParamAddr))
                {
                    LogHelper.SysLogWrite("Dlt645Plugin", "PublishPointData", $"{PluginName}：表[{deviceid}]应答DI[{di:X8}]与请求[{point.ParamAddr:X8}]不符，丢弃。", "DLT645插件");
                    return;
                }
                var valuebytes = new byte[data.Length - dilen];
                Array.Copy(data, dilen, valuebytes, 0, valuebytes.Length);
                if (point.CollectRegLength > 0 && valuebytes.Length > point.CollectRegLength)
                {
                    // 部分表应答含冗余尾字节,按点表配置字节数截取
                    valuebytes = valuebytes.Take(point.CollectRegLength).ToArray();
                }
                bool signed = string.Equals(point.CollectDataType?.Trim(), "bcdsigned", StringComparison.OrdinalIgnoreCase);
                string raw = Dlt645FrameHelper.DecodeBcdValue(valuebytes, signed);
                if (raw.IsZxxNullOrEmpty()) return;

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [point.ParamCode] = raw };
                var devicedata = BuildDeviceData(binding, values);
                if (devicedata == null) return;
                _ = SendMessageAsync(new PluginMessage
                {
                    MessageType = PluginMessageEnum.协议解析,
                    MessageJson = new List<DeviceData> { devicedata }.ToJson()
                });
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Dlt645Plugin", "PublishPointData", ex.ToString(), "DLT645插件"); }
        }

        /// <summary>
        /// 按设备参数模板构建DeviceData(BCD原始数字串经ParamFormula定标,如a*0.01;
        /// 按最大/最小合法值标记IsAlarm)
        /// </summary>
        private DeviceData? BuildDeviceData(Dlt645DeviceBinding binding, Dictionary<string, string> values)
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
        /// 发布端点下所有电表的运行状态(2在线/0离线,连接建立与断开时触发)
        /// </summary>
        private void PublishRunStateByEndpoint(string endpoint, int state)
        {
            try
            {
                List<Dlt645DeviceBinding> bindings;
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
                LogHelper.SysLogWrite("Dlt645Plugin", "PublishRunStateByEndpoint", $"{PluginName}：{endpoint}{(state == 2 ? "上线" : "离线")}，涉及{datalist.Count}块表。", "DLT645插件");
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Dlt645Plugin", "PublishRunStateByEndpoint", ex.ToString(), "DLT645插件"); }
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
                    LogHelper.SysLogWrite("Dlt645Plugin", "ReceiveMessageAsync", $"{PluginName}：收到心跳。", "DLT645插件");
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
                    LogHelper.SysLogWrite("Dlt645Plugin", "RestartForConfigUpdateAsync", $"{PluginName}：收到配置更新，重建采集拓扑。", "DLT645插件");
                    await PluginStop();
                    await PluginStart(_config?.ToJson() ?? "");
                }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Dlt645Plugin", "RestartForConfigUpdateAsync", ex.ToString(), "DLT645插件"); }
            finally { _restartGate.Release(); }
        }

        /// <summary>
        /// 处理设备控制(netdlt645timesync:向指令涉及设备所在的全部端点广播校时,
        /// C=08H无应答;加速抄读netdlt645read:重置目标表抄读指令的下次发送时刻)
        /// </summary>
        private async Task HandleDeviceControlAsync(string? messagejson)
        {
            if (messagejson.IsZxxNullOrEmpty())
            {
                LogHelper.SysLogWrite("Dlt645Plugin", "HandleDeviceControlAsync", $"{PluginName}：设备控制消息为空。", "DLT645插件");
                return;
            }
            Dlt645ControlCommand? command;
            try { command = messagejson.ToObject<Dlt645ControlCommand>(); }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Dlt645Plugin", "HandleDeviceControlAsync", ex.ToString(), "DLT645插件"); return; }
            if (command == null)
            {
                LogHelper.SysLogWrite("Dlt645Plugin", "HandleDeviceControlAsync", $"{PluginName}：设备控制消息格式无效。", "DLT645插件");
                return;
            }
            switch (command.ClassName?.Trim().ToLowerInvariant())
            {
                case "netdlt645timesync":
                    BroadcastTimeSync(command);
                    break;
                case "netdlt645read":
                    AccelerateRead(command);
                    break;
                default:
                    LogHelper.SysLogWrite("Dlt645Plugin", "HandleDeviceControlAsync", $"{PluginName}：不支持的控制类型[{command.ClassName}]，CommandId={command.CommandId}。", "DLT645插件");
                    break;
            }
        }

        /// <summary>
        /// 向目标设备所在端点广播校时(无应答一次性指令,同端点只广播一次)
        /// </summary>
        private void BroadcastTimeSync(Dlt645ControlCommand command)
        {
            var endpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var engines = new Dictionary<string, ChannelCommandEngine>(StringComparer.OrdinalIgnoreCase);
            lock (_bindingLock)
            {
                foreach (var deviceid in command.DeviceIds.Distinct())
                {
                    if (!_deviceMap.TryGetValue(deviceid, out var binding)) continue;
                    if (!endpoints.Add(binding.Endpoint)) continue;
                    var engine = binding.IsTcpMode ? _clientEngine : _serverEngine;
                    if (engine != null) engines[binding.Endpoint] = engine;
                }
            }
            foreach (var kv in engines)
            {
                kv.Value.Enqueue(new DriverCommand
                {
                    CmdKind = DriverCommand.KindControl,
                    Endpoint = kv.Key,
                    Payload = Dlt645FrameHelper.BuildTimeSyncFrame(DateTime.Now),
                    WaitForResponse = false,
                    OneShot = true,
                    CommandId = command.CommandId,
                    ClassName = "NetDlt645TimeSync"
                });
                LogHelper.SysLogWrite("Dlt645Plugin", "BroadcastTimeSync", $"{PluginName}：广播校时入队，端点[{kv.Key}]。", "DLT645插件");
            }
        }

        /// <summary>
        /// 加速抄读:重置目标表全部抄读指令的下次发送时刻为当前
        /// </summary>
        private void AccelerateRead(Dlt645ControlCommand command)
        {
            lock (_bindingLock)
            {
                foreach (var deviceid in command.DeviceIds.Distinct())
                {
                    if (!_deviceMap.TryGetValue(deviceid, out var binding)) continue;
                    var engine = binding.IsTcpMode ? _clientEngine : _serverEngine;
                    engine?.AccelerateCollect(binding.Endpoint, binding.Device.DeviceAdr);
                    LogHelper.SysLogWrite("Dlt645Plugin", "AccelerateRead", $"{PluginName}：加速抄读，表[{deviceid}]。", "DLT645插件");
                }
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

    /// <summary>
    /// 645设备控制命令(netdlt645timesync广播校时/netdlt645read加速抄读)
    /// </summary>
    internal sealed class Dlt645ControlCommand : PluginCommandBase
    {
        /// <summary>
        /// 控制类名
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 控制内容JSON(预留)
        /// </summary>
        public string ConContent { get; set; } = "";
    }
}
