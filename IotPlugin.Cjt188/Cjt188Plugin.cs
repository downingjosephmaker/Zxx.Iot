using System.Globalization;
using CenBoCommon.Zxx;
using CenboEventBus;
using IotDriverCore;
using IotLog;
using IotModel;
using NewLife;
using NewLife.Threading;

namespace IotPlugin.Cjt188
{
    /// <summary>
    /// CJ/T 188水表采集插件(M3.4:与645共享状态机骨架,差异在7字节地址+表型T/明文BCD无偏移/
    /// 2字节DI+SER序列号;一个DI应答含多值,点位按CollectBitOffset字节偏移+CollectRegLength切片;
    /// 同DI点位分组为一条抄读指令;表型T按类型编码经MeterTypeMap配置(默认0x10冷水表);
    /// 阀控C=04H DI=A017H走EnableValveControl白名单,默认关闭)
    /// </summary>
    public class Cjt188Plugin : ICenBoPlugin, ISimulatable
    {
        // ===== 模拟人格(方案A:独立端口/生命周期,委托Cjt188Simulator) =====
        private readonly Sim.Cjt188Simulator _simulator = new();

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
        private Cjt188PluginConfig? _config;
        private TimerX? _heartTimer;

        /// <summary>DTU透传服务端通道(拨入)</summary>
        private TcpServerChannel? _serverChannel;

        /// <summary>TCP客户端通道池(拨出)</summary>
        private TcpClientChannelPool? _clientPool;

        private ChannelCommandEngine? _serverEngine;
        private ChannelCommandEngine? _clientEngine;

        /// <summary>188拆帧定界器(68定界+长度域,服务端/客户端两路共用,断连清缓冲)</summary>
        private readonly FrameAccumulator _accumulator = new(FrameAccumulator.ExtractCjt188);

        private readonly object _bindingLock = new();

        /// <summary>设备ID→绑定</summary>
        private Dictionary<int, Cjt188DeviceBinding> _deviceMap = new();

        /// <summary>端点→绑定清单(一条DTU总线可挂多表)</summary>
        private Dictionary<string, List<Cjt188DeviceBinding>> _endpointMap = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>SER序列号发号器</summary>
        private int _serial;

        /// <summary>
        /// 188水表绑定(设备+实时参数模板+点表+表型+通道模式)
        /// </summary>
        private class Cjt188DeviceBinding
        {
            public DeviceInfo Device = null!;
            public DeviceParamEntity? DeviceParam;
            public List<DeviceTypeParam> Points = new();
            public byte MeterType = 0x10;
            public bool IsTcpMode;
            public string Endpoint = "";
            public byte[] AddressBcd = Array.Empty<byte>();
        }

        /// <summary>
        /// 抄读指令上下文(同DI点位分组)
        /// </summary>
        private class ReadContext
        {
            public ushort Di;
            public List<DeviceTypeParam> Points = new();
        }

        public void PluginInit(IEventBus<PluginEvent> eventBus) => _eventBus = eventBus;

        public string PluginName => "Cjt188Plugin";
        public string PluginType => "CJT188水表采集插件";
        public string PluginDesc => "CJ/T 188-2004/2018水表抄读与阀控(基于IotDriverCore驱动框架)";
        public string PluginVersion => "1.0.0";
        public string PluginGuid => "d0e6f2a8b4c5739d6ef07a8b1234d567";
        public string PluginModelPath => "";

        /// <summary>
        /// 插件自描述清单(B-1.3:配置schema+缺省配置+控制命令清单+寻址说明,
        /// 宿主在上传/加载时持久化到sys_plugin.plugin_manifest)
        /// </summary>
        public string PluginManifest => PluginMetaBuilder.BuildManifest(
            Cjt188PluginConfig.Current,
            new[]
            {
                new PluginMetaBuilder.PluginCommandMeta("netcjt188valve", "阀控(受配置阀控白名单开关约束,ConContent={\"ValveState\":1开/0关})"),
                new PluginMetaBuilder.PluginCommandMeta("netcjt188read", "加速抄读(重置目标表抄读指令的下次发送时刻)")
            },
            "点表寻址:ParamAddr=DI标识,表地址=DeviceInfo.DeviceAdr派生7字节BCD,表型T按类型编码映射;DevicePort=0=DTU透传拨入",
            "cjt188");

        #region 启动/停止

        /// <summary>
        /// 解析插件配置(B-1.1配置单轨化:DB plugin_config为唯一事实源;
        /// 传入为空时回落本地Config文件仅作首次迁移来源,JSON解析失败返回null由启动失败兜底)
        /// </summary>
        private Cjt188PluginConfig? LoadConfig(string _PluginConfig)
        {
            if (_PluginConfig.IsZxxNullOrEmpty()) return Cjt188PluginConfig.Current;
            try
            {
                return _PluginConfig.ToObject<Cjt188PluginConfig>();
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("Cjt188Plugin", "LoadConfig", ex.ToString(), "CJT188插件");
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
                    LogHelper.SysLogWrite("Cjt188Plugin", "PluginStart", $"{PluginName}：配置加载失败，插件启动失败。", "CJT188插件");
                    return false;
                }
                if (_config.DeviceTypeCodes.IsZxxNullOrEmpty())
                {
                    LogHelper.SysLogWrite("Cjt188Plugin", "PluginStart", $"{PluginName}：未配置设备类型编码，插件不启用。", "CJT188插件");
                    return false;
                }
                if (_config.SendIntervalMs <= 0 || _config.CmdTimeoutSeconds <= 0 || _config.CollectCycleMs <= 0)
                {
                    LogHelper.SysLogWrite("Cjt188Plugin", "PluginStart", $"{PluginName}：配置参数存在非正数，插件启动失败。", "CJT188插件");
                    return false;
                }
                if (!RefreshBindings(out var err))
                {
                    LogHelper.SysLogWrite("Cjt188Plugin", "PluginStart", $"{PluginName}：初始化设备失败，{err}", "CJT188插件");
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
                        LogHelper.SysLogWrite("Cjt188Plugin", "PluginStart", $"{PluginName}：存在DTU拨入设备但NetPort未配置，相关设备不采集。", "CJT188插件");
                    }
                    else
                    {
                        _serverChannel = new TcpServerChannel(_config.NetPort)
                        {
                            EndpointResolver = ResolveServerEndpoint,
                            // DTU注册包模式(§6.6:配置启用后拨入连接先发ASCII注册ID匹配DeviceGateway)
                            RegistrationResolver = _config.EnableDtuRegistration ? new DtuRegistrationHandler(ResolveDtuRegistration) : null,
                            // 188按68定界拆帧重组(§6.4:粘包拆多帧/半包留缓冲等待)
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
                LogHelper.SysLogWrite("Cjt188Plugin", "PluginStart", $"{PluginName}：插件启动成功。", "CJT188插件");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("Cjt188Plugin", "PluginStart", ex.ToString(), "CJT188插件");
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
                    _deviceMap = new Dictionary<int, Cjt188DeviceBinding>();
                    _endpointMap = new Dictionary<string, List<Cjt188DeviceBinding>>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Cjt188Plugin", "PluginStop", ex.ToString(), "CJT188插件"); }
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
            catch (Exception ex) { LogHelper.ErrorLogWrite("Cjt188Plugin", "SendMessageAsync", ex.ToString(), "CJT188插件"); }
        }

        #endregion

        #region 设备初始化

        /// <summary>
        /// 从数据库加载水表绑定与点表并原子替换内存映射(点表条件:CollectFuncCode>0即启用)
        /// </summary>
        private bool RefreshBindings(out string error)
        {
            error = "";
            var typecodes = _config!.DeviceTypeCodes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            var metertypemap = ParseMeterTypeMap(_config.MeterTypeMap);
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

            var devicemap = new Dictionary<int, Cjt188DeviceBinding>();
            var endpointmap = new Dictionary<string, List<Cjt188DeviceBinding>>(StringComparer.OrdinalIgnoreCase);
            foreach (var device in devices)
            {
                if (device.DeviceIp.IsZxxNullOrEmpty())
                {
                    LogHelper.SysLogWrite("Cjt188Plugin", "RefreshBindings", $"{PluginName}：设备[{device.DeviceId}({device.DeviceName})]未配置IP，跳过。", "CJT188插件");
                    continue;
                }
                if (!pointmap.TryGetValue(device.DeviceTypeCode, out var points) || !points.IsZxxAny())
                {
                    LogHelper.SysLogWrite("Cjt188Plugin", "RefreshBindings", $"{PluginName}：类型[{device.DeviceTypeCode}]无DI点表配置，设备[{device.DeviceId}]跳过。", "CJT188插件");
                    continue;
                }
                bool tcpmode = device.DevicePort > 0;
                var binding = new Cjt188DeviceBinding
                {
                    Device = device,
                    DeviceParam = devparams?.Find(t => t.DeviceId == device.DeviceId),
                    Points = points,
                    MeterType = metertypemap.TryGetValue(device.DeviceTypeCode, out var mt) ? mt : (byte)0x10,
                    IsTcpMode = tcpmode,
                    Endpoint = tcpmode ? $"{device.DeviceIp}:{device.DevicePort}" : device.DeviceIp,
                    AddressBcd = Cjt188FrameHelper.BuildAddressBcd(device.DeviceAdr)
                };
                devicemap[device.DeviceId] = binding;
                if (!endpointmap.TryGetValue(binding.Endpoint, out var list))
                {
                    list = new List<Cjt188DeviceBinding>();
                    endpointmap[binding.Endpoint] = list;
                }
                list.Add(binding);
            }
            if (!devicemap.Any())
            {
                error = "未构建任何有效水表绑定。";
                return false;
            }

            lock (_bindingLock)
            {
                _deviceMap = devicemap;
                _endpointMap = endpointmap;
            }
            LogHelper.SysLogWrite("Cjt188Plugin", "RefreshBindings", $"{PluginName}：水表映射初始化完成，{devicemap.Count}块表，{endpointmap.Count}个端点。", "CJT188插件");
            return true;
        }

        /// <summary>
        /// 解析类型编码→表型T映射配置("类型编码:表型十进制"逗号分隔)
        /// </summary>
        private static Dictionary<string, byte> ParseMeterTypeMap(string config)
        {
            var result = new Dictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            if (config.IsZxxNullOrEmpty()) return result;
            foreach (var pair in config.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = pair.Split(':', StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && byte.TryParse(parts[1], out var mt)) result[parts[0]] = mt;
            }
            return result;
        }

        /// <summary>
        /// 按端点重建抄读指令队列(同DI点位分组为一条指令,一个DI应答含多值)
        /// </summary>
        private void RebuildCollectQueues()
        {
            Dictionary<string, List<Cjt188DeviceBinding>> snapshot;
            lock (_bindingLock) { snapshot = _endpointMap.ToDictionary(t => t.Key, t => t.Value.ToList(), StringComparer.OrdinalIgnoreCase); }
            foreach (var kv in snapshot)
            {
                var engine = kv.Value[0].IsTcpMode ? _clientEngine : _serverEngine;
                if (engine == null) continue;
                var cmds = new List<DriverCommand>();
                foreach (var binding in kv.Value)
                {
                    foreach (var group in binding.Points.GroupBy(t => unchecked((ushort)t.ParamAddr)))
                    {
                        cmds.Add(new DriverCommand
                        {
                            CmdKind = DriverCommand.KindCollect,
                            Endpoint = binding.Endpoint,
                            DeviceId = binding.Device.DeviceId,
                            DeviceAddr = binding.Device.DeviceAdr,
                            Payload = Cjt188FrameHelper.BuildReadFrame(binding.MeterType, binding.AddressBcd, group.Key, NextSer()),
                            ResponseMatcher = BuildMatcher(binding, Cjt188FrameHelper.ReadCode),
                            TimeoutSeconds = _config!.CmdTimeoutSeconds,
                            CycleMs = _config.CollectCycleMs,
                            Tag = new ReadContext { Di = group.Key, Points = group.ToList() }
                        });
                    }
                }
                if (cmds.IsZxxAny()) engine.ReplaceQueue(kv.Key, cmds);
            }
        }

        /// <summary>
        /// 应答匹配器(帧内7字节表地址一致且控制码为C|0x80应答)
        /// </summary>
        private static Func<byte[], bool> BuildMatcher(Cjt188DeviceBinding binding, byte reqcode)
        {
            var addr = binding.AddressBcd;
            return frame =>
            {
                if (!Cjt188FrameHelper.TryParseFrame(frame, out _, out var a, out var c, out _)) return false;
                if (!a.SequenceEqual(addr)) return false;
                return c == (byte)(reqcode | 0x80);
            };
        }

        /// <summary>
        /// 取下一个SER序列号
        /// </summary>
        private byte NextSer() => (byte)Interlocked.Increment(ref _serial);

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
                LogHelper.SysLogWrite("Cjt188Plugin", "ResolveDtuRegistration", $"{PluginName}：DTU注册ID[{regid}]未匹配任何设备网关编号，等待注册超时。", "CJT188插件");
                return null;
            }
            consumed = end;
            while (consumed < data.Length && (data[consumed] == 0x0D || data[consumed] == 0x0A)) consumed++;
            LogHelper.SysLogWrite("Cjt188Plugin", "ResolveDtuRegistration", $"{PluginName}：DTU注册[{regid}]绑定端点[{endpoint}]。", "CJT188插件");
            return endpoint;
        }

        #endregion

        #region 收帧处理

        /// <summary>
        /// 入站帧核心处理:帧校验→回执匹配→阀控回执/抄读数据分发
        /// (粘包拆帧待M3.5声明式定界器)
        /// </summary>
        private void OnFrame(ChannelCommandEngine? engine, string endpoint, byte[] buffer)
        {
            try
            {
                if (engine == null) return;
                if (!Cjt188FrameHelper.TryParseFrame(buffer, out _, out _, out _, out var data))
                {
                    LogHelper.SysLogWrite("Cjt188Plugin", "OnFrame", $"{PluginName}：帧校验失败，来自{endpoint}，{buffer.ToHex()}", "CJT188插件");
                    return;
                }

                var cmd = engine.MatchResponse(endpoint, buffer);
                if (cmd == null)
                {
                    LogHelper.SysLogWrite("Cjt188Plugin", "OnFrame", $"{PluginName}：未匹配到等待指令，来自{endpoint}，{buffer.ToHex()}", "CJT188插件");
                    return;
                }

                // 阀控回执(C=84H,匹配即成功)
                if (cmd.CmdKind == DriverCommand.KindControl)
                {
                    LogHelper.SysLogWrite("Cjt188Plugin", "OnFrame", $"{PluginName}：阀控回执成功，表[{cmd.DeviceId}]。", "CJT188插件");
                    PublishControlResult(cmd, true, "阀控成功");
                    engine.Remove(cmd);
                    return;
                }

                if (cmd.Tag is not ReadContext context) return;
                _ = Task.Run(() => PublishReadData(cmd.DeviceId, context, data));
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Cjt188Plugin", "OnFrame", ex.ToString(), "CJT188插件"); }
        }

        /// <summary>
        /// 控制指令最终超时回调
        /// </summary>
        private void OnCommandTimeout(DriverCommand cmd) => PublishControlResult(cmd, false, "指令超时");

        #endregion

        #region 数据解码与发布

        /// <summary>
        /// 校验应答DI并按点位切片解码(数据域=DI[2低位在前]+SER[1]+值区;
        /// 点位在值区内按CollectBitOffset字节偏移+CollectRegLength字节数切片;
        /// CollectDataType:bcd默认/bcdsigned符号位/bin二进制小端如状态字ST)
        /// </summary>
        private void PublishReadData(int deviceid, ReadContext context, byte[] data)
        {
            try
            {
                Cjt188DeviceBinding? binding;
                lock (_bindingLock) { _deviceMap.TryGetValue(deviceid, out binding); }
                if (binding == null) return;

                if (data.Length < 3) return;
                ushort di = (ushort)(data[0] | (data[1] << 8));
                if (di != context.Di)
                {
                    LogHelper.SysLogWrite("Cjt188Plugin", "PublishReadData", $"{PluginName}：表[{deviceid}]应答DI[{di:X4}]与请求[{context.Di:X4}]不符，丢弃。", "CJT188插件");
                    return;
                }
                var valuearea = new byte[data.Length - 3];
                Array.Copy(data, 3, valuearea, 0, valuearea.Length);

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var point in context.Points)
                {
                    int offset = Math.Max(0, point.CollectBitOffset);
                    int length = point.CollectRegLength > 0 ? point.CollectRegLength : 4;
                    if (offset + length > valuearea.Length) continue;
                    var slice = new byte[length];
                    Array.Copy(valuearea, offset, slice, 0, length);
                    var datatype = point.CollectDataType?.Trim().ToLowerInvariant() ?? "";
                    values[point.ParamCode] = datatype switch
                    {
                        "bin" => Cjt188FrameHelper.DecodeBinValue(slice),
                        "bcdsigned" => Cjt188FrameHelper.DecodeBcdValue(slice, true),
                        _ => Cjt188FrameHelper.DecodeBcdValue(slice, false)
                    };
                }
                if (!values.Any()) return;

                var devicedata = BuildDeviceData(binding, values);
                if (devicedata == null) return;
                _ = SendMessageAsync(new PluginMessage
                {
                    MessageType = PluginMessageEnum.协议解析,
                    MessageJson = new List<DeviceData> { devicedata }.ToJson()
                });
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Cjt188Plugin", "PublishReadData", ex.ToString(), "CJT188插件"); }
        }

        /// <summary>
        /// 按设备参数模板构建DeviceData(原始数字串经ParamFormula定标,按最大/最小合法值标记IsAlarm)
        /// </summary>
        private DeviceData? BuildDeviceData(Cjt188DeviceBinding binding, Dictionary<string, string> values)
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
        /// 发布端点下所有水表的运行状态(2在线/0离线,连接建立与断开时触发)
        /// </summary>
        private void PublishRunStateByEndpoint(string endpoint, int state)
        {
            try
            {
                List<Cjt188DeviceBinding> bindings;
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
                LogHelper.SysLogWrite("Cjt188Plugin", "PublishRunStateByEndpoint", $"{PluginName}：{endpoint}{(state == 2 ? "上线" : "离线")}，涉及{datalist.Count}块表。", "CJT188插件");
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Cjt188Plugin", "PublishRunStateByEndpoint", ex.ToString(), "CJT188插件"); }
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
                    LogHelper.SysLogWrite("Cjt188Plugin", "ReceiveMessageAsync", $"{PluginName}：收到心跳。", "CJT188插件");
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
                    LogHelper.SysLogWrite("Cjt188Plugin", "RestartForConfigUpdateAsync", $"{PluginName}：收到配置更新，重建采集拓扑。", "CJT188插件");
                    await PluginStop();
                    await PluginStart(_config?.ToJson() ?? "");
                }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Cjt188Plugin", "RestartForConfigUpdateAsync", ex.ToString(), "CJT188插件"); }
            finally { _restartGate.Release(); }
        }

        /// <summary>
        /// 处理设备控制(netcjt188valve阀控走白名单开关,ConContent={"ValveState":1开/0关};
        /// netcjt188read加速抄读)
        /// </summary>
        private async Task HandleDeviceControlAsync(string? messagejson)
        {
            if (messagejson.IsZxxNullOrEmpty())
            {
                LogHelper.SysLogWrite("Cjt188Plugin", "HandleDeviceControlAsync", $"{PluginName}：设备控制消息为空。", "CJT188插件");
                return;
            }
            Cjt188ControlCommand? command;
            try { command = messagejson.ToObject<Cjt188ControlCommand>(); }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Cjt188Plugin", "HandleDeviceControlAsync", ex.ToString(), "CJT188插件"); return; }
            if (command == null)
            {
                LogHelper.SysLogWrite("Cjt188Plugin", "HandleDeviceControlAsync", $"{PluginName}：设备控制消息格式无效。", "CJT188插件");
                return;
            }
            switch (command.ClassName?.Trim().ToLowerInvariant())
            {
                case "netcjt188valve":
                    await HandleValveControlAsync(command);
                    break;
                case "netcjt188read":
                    AccelerateRead(command);
                    break;
                default:
                    LogHelper.SysLogWrite("Cjt188Plugin", "HandleDeviceControlAsync", $"{PluginName}：不支持的控制类型[{command.ClassName}]，CommandId={command.CommandId}。", "CJT188插件");
                    break;
            }
        }

        /// <summary>
        /// 阀控下发(方案§6.3:命令白名单+二次确认——白名单即EnableValveControl开关,
        /// 二次确认由平台侧UI完成;C=04H DI=A017H,0x55开/0x99关)
        /// </summary>
        private async Task HandleValveControlAsync(Cjt188ControlCommand command)
        {
            if (!_config!.EnableValveControl)
            {
                LogHelper.SysLogWrite("Cjt188Plugin", "HandleValveControlAsync", $"{PluginName}：阀控白名单未开启，拒绝执行，CommandId={command.CommandId}。", "CJT188插件");
                foreach (var deviceid in command.DeviceIds.Distinct())
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, "", false, "阀控白名单未开启");
                }
                return;
            }
            NetCjt188Valve? model;
            try { model = command.ConContent.ToObject<NetCjt188Valve>(); }
            catch { LogHelper.SysLogWrite("Cjt188Plugin", "HandleValveControlAsync", $"{PluginName}：NetCjt188Valve解析失败。", "CJT188插件"); return; }
            if (model == null) return;

            foreach (var deviceid in command.DeviceIds.Distinct())
            {
                Cjt188DeviceBinding? binding;
                lock (_bindingLock) { _deviceMap.TryGetValue(deviceid, out binding); }
                if (binding == null)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, "", false, "未找到设备绑定信息");
                    continue;
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
                    Payload = Cjt188FrameHelper.BuildValveFrame(binding.MeterType, binding.AddressBcd, NextSer(), model.ValveState == 1),
                    ResponseMatcher = BuildMatcher(binding, Cjt188FrameHelper.WriteCode),
                    WaitForResponse = true,
                    OneShot = true,
                    TimeoutSeconds = _config.CmdTimeoutSeconds,
                    RetryLimit = _config.RetryLimit,
                    CommandId = command.CommandId,
                    ClassName = "NetCjt188Valve"
                });
                LogHelper.SysLogWrite("Cjt188Plugin", "HandleValveControlAsync", $"{PluginName}：阀控入队，表[{deviceid}]{(model.ValveState == 1 ? "开阀" : "关阀")}。", "CJT188插件");
            }
        }

        /// <summary>
        /// 加速抄读:重置目标表全部抄读指令的下次发送时刻为当前
        /// </summary>
        private void AccelerateRead(Cjt188ControlCommand command)
        {
            lock (_bindingLock)
            {
                foreach (var deviceid in command.DeviceIds.Distinct())
                {
                    if (!_deviceMap.TryGetValue(deviceid, out var binding)) continue;
                    var engine = binding.IsTcpMode ? _clientEngine : _serverEngine;
                    engine?.AccelerateCollect(binding.Endpoint, binding.Device.DeviceAdr);
                    LogHelper.SysLogWrite("Cjt188Plugin", "AccelerateRead", $"{PluginName}：加速抄读，表[{deviceid}]。", "CJT188插件");
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
    /// 188设备控制命令(netcjt188valve阀控/netcjt188read加速抄读)
    /// </summary>
    internal sealed class Cjt188ControlCommand : PluginCommandBase
    {
        /// <summary>
        /// 控制类名
        /// </summary>
        public string ClassName { get; set; } = "";

        /// <summary>
        /// 控制内容JSON(NetCjt188Valve)
        /// </summary>
        public string ConContent { get; set; } = "";
    }

    /// <summary>
    /// 阀控内容(1开阀/0关阀)
    /// </summary>
    internal sealed class NetCjt188Valve
    {
        /// <summary>
        /// 阀门状态(1开/0关)
        /// </summary>
        public int ValveState { get; set; }
    }
}
