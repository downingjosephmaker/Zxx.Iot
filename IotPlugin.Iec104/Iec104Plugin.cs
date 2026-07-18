using System.Globalization;
using CenBoCommon.Zxx;
using CenboEventBus;
using IotDriverCore;
using IotLog;
using IotModel;
using NewLife;
using NewLife.Threading;

namespace IotPlugin.Iec104
{
    /// <summary>
    /// IEC 60870-5-104主站采集控制插件(方案docs/IEC104协议插件方案.md;
    /// 与轮询问答型协议不同——TCP长连接拨出2404→STARTDT_act→总召唤拿全量→之后子站变化主动上报,
    /// 接收循环解I/S/U帧驱动状态机,数据到即经事件总线发布协议解析消息;
    /// 点表寻址:公共地址CA=DeviceInfo.DeviceAdr,信息体地址IOA=ParamAddr,类型标识TI=CollectFuncCode;
    /// 遥控45/46/50默认选择后执行SBO——方案§4.6)
    /// </summary>
    public class Iec104Plugin : ICenBoPlugin
    {
        private IEventBus<PluginEvent>? _eventBus;
        private Iec104PluginConfig? _config;
        private TimerX? _heartTimer;
        private TimerX? _tickTimer;

        /// <summary>子站TCP客户端通道池(平台拨出,断线退避重连由池承担=t0语义)</summary>
        private TcpClientChannelPool? _clientPool;

        /// <summary>104拆帧定界器(0x68+长度域,粘包拆多帧/半包留缓冲)</summary>
        private readonly FrameAccumulator _accumulator = new(Iec104FrameHelper.Extract104);

        /// <summary>端点→链路状态机(锁state实例访问)</summary>
        private readonly Dictionary<string, Iec104StateMachine> _states = new(StringComparer.OrdinalIgnoreCase);

        private readonly object _bindingLock = new();

        /// <summary>设备ID→绑定</summary>
        private Dictionary<int, Iec104DeviceBinding> _deviceMap = new();

        /// <summary>端点→绑定清单(一条链路可挂多个公共地址CA)</summary>
        private Dictionary<string, List<Iec104DeviceBinding>> _endpointMap = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// IEC104设备绑定(设备+实时参数模板+点表+链路端点;CA=DeviceAdr)
        /// </summary>
        private class Iec104DeviceBinding
        {
            public DeviceInfo Device = null!;
            public DeviceParamEntity? DeviceParam;
            public List<DeviceTypeParam> Points = new();
            public string Endpoint = "";
            public int Ca;
        }

        public void PluginInit(IEventBus<PluginEvent> eventBus) => _eventBus = eventBus;

        public string PluginName => "Iec104Plugin";
        public string PluginType => "IEC104主站采集插件";
        public string PluginDesc => "IEC 60870-5-104主站采集与遥控(长连接主动上报+总召唤,基于IotDriverCore驱动框架)";
        public string PluginVersion => "1.0.0";
        public string PluginGuid => "e7a91c3d5b2f4680a1c2d3e4f5061728";
        public string PluginModelPath => "";

        /// <summary>
        /// 插件自描述清单(配置schema+缺省配置+控制命令清单+寻址说明,宿主持久化到sys_plugin.plugin_manifest)
        /// </summary>
        public string PluginManifest => PluginMetaBuilder.BuildManifest(
            Iec104PluginConfig.Current,
            new[]
            {
                new PluginMetaBuilder.PluginCommandMeta("netiec104write",
                    "IEC104写点位(TI=1/30经单点命令45,TI=3/31经双点命令46,TI=9/11/13/34/36经短浮点设定值50下发,默认选择后执行SBO)")
            },
            "点表寻址:公共地址CA=设备通讯地址DeviceAdr,信息体地址IOA=ParamAddr,类型标识TI=CollectFuncCode" +
            "(1单点/3双点/9归一化/11标度化/13短浮点,30/31/34/36带时标);设备须配DeviceIp,DevicePort为0时用默认端口2404",
            "iec104");

        #region 启动/停止

        /// <summary>
        /// 解析插件配置(DB plugin_config为唯一事实源;传入为空时回落本地Config文件仅作首次迁移来源)
        /// </summary>
        private Iec104PluginConfig? LoadConfig(string _PluginConfig)
        {
            if (_PluginConfig.IsZxxNullOrEmpty()) return Iec104PluginConfig.Current;
            try
            {
                return _PluginConfig.ToObject<Iec104PluginConfig>();
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("Iec104Plugin", "LoadConfig", ex.ToString(), "IEC104插件");
                return null;
            }
        }

        /// <summary>
        /// 启动插件:校验配置→加载设备绑定与点表→拉起客户端通道池→启动定时器
        /// </summary>
        public async Task<bool> PluginStart(string _PluginConfig)
        {
            try
            {
                _config = LoadConfig(_PluginConfig);
                if (_config == null)
                {
                    LogHelper.SysLogWrite("Iec104Plugin", "PluginStart", $"{PluginName}：配置加载失败，插件启动失败。", "IEC104插件");
                    return false;
                }
                if (_config.DeviceTypeCodes.IsZxxNullOrEmpty())
                {
                    LogHelper.SysLogWrite("Iec104Plugin", "PluginStart", $"{PluginName}：未配置设备类型编码，插件不启用。", "IEC104插件");
                    return false;
                }
                if (_config.K <= 0 || _config.W <= 0 || _config.T1Seconds <= 0 || _config.T2Seconds <= 0 || _config.T3Seconds <= 0)
                {
                    LogHelper.SysLogWrite("Iec104Plugin", "PluginStart", $"{PluginName}：k/w/t1~t3存在非正数，插件启动失败。", "IEC104插件");
                    return false;
                }
                if (_config.T2Seconds >= _config.T1Seconds)
                {
                    LogHelper.SysLogWrite("Iec104Plugin", "PluginStart", $"{PluginName}：t2须小于t1(当前t2={_config.T2Seconds},t1={_config.T1Seconds})，插件启动失败。", "IEC104插件");
                    return false;
                }
                if (!RefreshBindings(out var err))
                {
                    LogHelper.SysLogWrite("Iec104Plugin", "PluginStart", $"{PluginName}：初始化设备失败，{err}", "IEC104插件");
                    return false;
                }

                _clientPool = new TcpClientChannelPool
                {
                    FrameReceived = (ep, data) =>
                    {
                        foreach (var frame in _accumulator.Push(ep, data))
                        {
                            HandleFrame(ep, frame);
                        }
                    },
                    SessionOpened = OnSessionOpened,
                    SessionClosed = OnSessionClosed
                };
                lock (_bindingLock)
                {
                    foreach (var kv in _endpointMap)
                    {
                        var device = kv.Value[0].Device;
                        int port = device.DevicePort > 0 ? device.DevicePort : _config.DefaultPort;
                        _clientPool.AddEndpoint(kv.Key, device.DeviceIp, port);
                    }
                }
                _clientPool.Start();

                _tickTimer?.Dispose();
                _tickTimer = new TimerX(CheckTimers, null, 1_000, 1_000);
                _heartTimer?.Dispose();
                _heartTimer = new TimerX(HeartBeatDown, null, 5_000, _config.HeartSecond * 1_000);
                LogHelper.SysLogWrite("Iec104Plugin", "PluginStart", $"{PluginName}：插件启动成功。", "IEC104插件");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("Iec104Plugin", "PluginStart", ex.ToString(), "IEC104插件");
                return false;
            }
        }

        /// <summary>
        /// 停止插件:销毁定时器→释放通道池→清空绑定与状态机
        /// </summary>
        public async Task<bool> PluginStop()
        {
            try
            {
                _heartTimer?.Dispose();
                _heartTimer = null;
                _tickTimer?.Dispose();
                _tickTimer = null;
                _clientPool?.Dispose();
                _clientPool = null;
                lock (_states) { _states.Clear(); }
                lock (_bindingLock)
                {
                    _deviceMap = new Dictionary<int, Iec104DeviceBinding>();
                    _endpointMap = new Dictionary<string, List<Iec104DeviceBinding>>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Iec104Plugin", "PluginStop", ex.ToString(), "IEC104插件"); }
            return true;
        }

        #endregion

        #region 设备初始化

        /// <summary>
        /// 从数据库加载设备绑定与点表并原子替换内存映射
        /// (点表条件:类型编码归属本插件且collect_func_code为支持的监视方向TI)
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
                .Where(t => typecodes.Contains(t.DeviceTypeCode) && Iec104FrameHelper.IsMonitorType((byte)t.CollectFuncCode))
                .GroupBy(t => t.DeviceTypeCode)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var deviceids = devices.Select(t => t.DeviceId).ToList();
            var devparams = DeviceParamDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));

            var devicemap = new Dictionary<int, Iec104DeviceBinding>();
            var endpointmap = new Dictionary<string, List<Iec104DeviceBinding>>(StringComparer.OrdinalIgnoreCase);
            foreach (var device in devices)
            {
                if (device.DeviceIp.IsZxxNullOrEmpty())
                {
                    LogHelper.SysLogWrite("Iec104Plugin", "RefreshBindings", $"{PluginName}：设备[{device.DeviceId}({device.DeviceName})]未配置IP，跳过。", "IEC104插件");
                    continue;
                }
                if (!pointmap.TryGetValue(device.DeviceTypeCode, out var points) || !points.IsZxxAny())
                {
                    LogHelper.SysLogWrite("Iec104Plugin", "RefreshBindings", $"{PluginName}：类型[{device.DeviceTypeCode}]无IEC104点表配置，设备[{device.DeviceId}]跳过。", "IEC104插件");
                    continue;
                }
                int port = device.DevicePort > 0 ? device.DevicePort : _config.DefaultPort;
                var binding = new Iec104DeviceBinding
                {
                    Device = device,
                    DeviceParam = devparams?.Find(t => t.DeviceId == device.DeviceId),
                    Points = points,
                    Endpoint = $"{device.DeviceIp}:{port}",
                    Ca = device.DeviceAdr
                };
                devicemap[device.DeviceId] = binding;
                if (!endpointmap.TryGetValue(binding.Endpoint, out var list))
                {
                    list = new List<Iec104DeviceBinding>();
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
            LogHelper.SysLogWrite("Iec104Plugin", "RefreshBindings", $"{PluginName}：设备映射初始化完成，{devicemap.Count}台设备，{endpointmap.Count}条链路。", "IEC104插件");
            return true;
        }

        #endregion

        #region 链路生命周期

        /// <summary>
        /// 取端点状态机(不存在则创建)
        /// </summary>
        private Iec104StateMachine GetState(string endpoint)
        {
            lock (_states)
            {
                if (!_states.TryGetValue(endpoint, out var state))
                {
                    state = new Iec104StateMachine();
                    _states[endpoint] = state;
                }
                return state;
            }
        }

        /// <summary>
        /// 连接建立:复位状态机→发STARTDT_act(方案§4.5启动时序第一步)
        /// </summary>
        private void OnSessionOpened(string endpoint)
        {
            var state = GetState(endpoint);
            lock (state)
            {
                state.Reset();
                state.StartDtSent = DateTime.Now;
                SendRaw(endpoint, state, Iec104FrameHelper.BuildUFrame(Iec104FrameHelper.StartDtAct));
            }
            PublishRunStateByEndpoint(endpoint, 2);
        }

        /// <summary>
        /// 连接断开:清拆帧缓冲→挂起控制判失败→复位状态机→发布离线
        /// (断链即全量失效,重连后STARTDT_con必触发总召唤刷新——方案§7缓存陈旧缓解)
        /// </summary>
        private void OnSessionClosed(string endpoint)
        {
            _accumulator.Reset(endpoint);
            var state = GetState(endpoint);
            PendingControl? control;
            lock (state)
            {
                control = state.Control;
                state.Reset();
            }
            if (control != null)
            {
                _ = PublishControlResultAsync(control.CommandId, control.DeviceId, control.DeviceName, false, "链路断开，控制中止");
            }
            PublishRunStateByEndpoint(endpoint, 0);
        }

        /// <summary>
        /// 协议层判死强制断链(t1超时/序号错乱;通道池连接循环随退避自动重连)
        /// </summary>
        private void ForceReconnect(string endpoint, string reason)
        {
            LogHelper.SysLogWrite("Iec104Plugin", "ForceReconnect", $"{PluginName}：链路[{endpoint}]{reason}，强制断链重连。", "IEC104插件");
            _clientPool?.Disconnect(endpoint);
        }

        #endregion

        #region 发送

        /// <summary>
        /// 发送原始帧并刷新发送时刻(调用方须持有state锁)
        /// </summary>
        private bool SendRaw(string endpoint, Iec104StateMachine state, byte[] frame)
        {
            bool ok = _clientPool?.Send(endpoint, frame) ?? false;
            if (ok) state.LastTx = DateTime.Now;
            return ok;
        }

        /// <summary>
        /// 发送I帧(占用发送序号并入未确认队列;发I帧天然捎带确认,清接收欠账;
        /// k窗口到顶拒发——方案§4.4;调用方须持有state锁)
        /// </summary>
        private bool SendIFrame(string endpoint, Iec104StateMachine state, byte[] asdu)
        {
            if (!state.CanSendI(_config!.K)) return false;
            int seq = state.NextSendSeq();
            var frame = Iec104FrameHelper.BuildIFrame(seq, state.Vr, asdu);
            state.RecvSinceAck = 0;
            state.FirstUnackedRx = null;
            state.PendingI.Enqueue((seq, DateTime.Now));
            return SendRaw(endpoint, state, frame);
        }

        /// <summary>
        /// 发送S帧确认(清接收欠账;调用方须持有state锁)
        /// </summary>
        private void SendSFrame(string endpoint, Iec104StateMachine state)
        {
            state.RecvSinceAck = 0;
            state.FirstUnackedRx = null;
            SendRaw(endpoint, state, Iec104FrameHelper.BuildSFrame(state.Vr));
        }

        /// <summary>
        /// 向链路上所有公共地址发总召唤(STARTDT_con后与周期重召共用;调用方须持有state锁)
        /// </summary>
        private void SendInterrogation(string endpoint, Iec104StateMachine state)
        {
            List<int> cas;
            lock (_bindingLock)
            {
                if (!_endpointMap.TryGetValue(endpoint, out var list)) return;
                cas = list.Select(b => b.Ca).Distinct().ToList();
            }
            foreach (int ca in cas)
            {
                if (_config!.EnableClockSync)
                {
                    SendIFrame(endpoint, state, Iec104FrameHelper.BuildClockSync(ca, DateTime.Now));
                }
                SendIFrame(endpoint, state, Iec104FrameHelper.BuildInterrogation(ca));
            }
            state.LastGi = DateTime.Now;
        }

        #endregion

        #region 收帧处理

        /// <summary>
        /// 入站帧核心处理:APCI解析→U帧状态机应答/S帧确认/I帧解ASDU三路分发(方案§2架构)
        /// </summary>
        private void HandleFrame(string endpoint, byte[] frame)
        {
            try
            {
                if (!Iec104FrameHelper.TryParseApci(frame, out var apci))
                {
                    LogHelper.SysLogWrite("Iec104Plugin", "HandleFrame", $"{PluginName}：APCI解析失败，来自{endpoint}，{frame.ToHex()}", "IEC104插件");
                    return;
                }
                var state = GetState(endpoint);
                Iec104Asdu? asdu = null;
                lock (state)
                {
                    state.LastRx = DateTime.Now;
                    switch (apci.Kind)
                    {
                        case 'U':
                            HandleUFrame(endpoint, state, apci.UCtrl);
                            return;
                        case 'S':
                            state.OnAck(apci.Nr);
                            return;
                        case 'I':
                            if (apci.Ns != state.Vr)
                            {
                                // 序号错乱按标准判链路失效(方案§7:k/w处理错误表现为跑一段时间后断链)
                                ForceReconnect(endpoint, $"I帧序号错乱(期望{state.Vr}实收{apci.Ns})");
                                return;
                            }
                            state.Vr = (apci.Ns + 1) % Iec104FrameHelper.SeqModulo;
                            state.OnAck(apci.Nr);
                            state.RecvSinceAck++;
                            state.FirstUnackedRx ??= DateTime.Now;
                            if (!Iec104FrameHelper.TryParseAsdu(apci.Asdu, out asdu))
                            {
                                LogHelper.SysLogWrite("Iec104Plugin", "HandleFrame", $"{PluginName}：ASDU解析失败，来自{endpoint}，{apci.Asdu.ToHex()}", "IEC104插件");
                                asdu = null;
                            }
                            // 收满w帧必发S帧确认(方案§4.4)
                            if (state.RecvSinceAck >= _config!.W)
                            {
                                SendSFrame(endpoint, state);
                            }
                            break;
                    }
                }
                if (asdu != null) HandleAsdu(endpoint, asdu);
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Iec104Plugin", "HandleFrame", ex.ToString(), "IEC104插件"); }
        }

        /// <summary>
        /// U帧处理:STARTDT_con→启动完成发总召唤;TESTFR_act→回con;TESTFR_con→清探活挂起
        /// (调用方须持有state锁)
        /// </summary>
        private void HandleUFrame(string endpoint, Iec104StateMachine state, byte uctrl)
        {
            switch (uctrl)
            {
                case Iec104FrameHelper.StartDtCon:
                    state.Started = true;
                    state.StartDtSent = null;
                    LogHelper.SysLogWrite("Iec104Plugin", "HandleUFrame", $"{PluginName}：链路[{endpoint}]STARTDT完成，发起总召唤。", "IEC104插件");
                    SendInterrogation(endpoint, state);
                    break;
                case Iec104FrameHelper.TestFrAct:
                    SendRaw(endpoint, state, Iec104FrameHelper.BuildUFrame(Iec104FrameHelper.TestFrCon));
                    break;
                case Iec104FrameHelper.TestFrCon:
                    state.TestFrSent = null;
                    break;
                case Iec104FrameHelper.StopDtCon:
                    state.Started = false;
                    break;
                default:
                    LogHelper.SysLogWrite("Iec104Plugin", "HandleUFrame", $"{PluginName}：链路[{endpoint}]收到未处理U帧0x{uctrl:X2}。", "IEC104插件");
                    break;
            }
        }

        /// <summary>
        /// ASDU分发:监视方向数据→解码发布;命令镜像45/46/50→SBO链推进;总召唤100→过程日志
        /// </summary>
        private void HandleAsdu(string endpoint, Iec104Asdu asdu)
        {
            if (Iec104FrameHelper.IsMonitorType(asdu.Ti))
            {
                PublishMonitorData(endpoint, asdu);
                return;
            }
            switch (asdu.Ti)
            {
                case Iec104FrameHelper.TiSingleCommand:
                case Iec104FrameHelper.TiDoubleCommand:
                case Iec104FrameHelper.TiSetpointFloat:
                    HandleControlMirror(endpoint, asdu);
                    break;
                case Iec104FrameHelper.TiInterrogation:
                    if (asdu.Cot == Iec104FrameHelper.CotActivationTerm)
                    {
                        LogHelper.SysLogWrite("Iec104Plugin", "HandleAsdu", $"{PluginName}：链路[{endpoint}]CA[{asdu.Ca}]总召唤激活终止(全量刷新完成)。", "IEC104插件");
                    }
                    else if (asdu.Negative)
                    {
                        LogHelper.SysLogWrite("Iec104Plugin", "HandleAsdu", $"{PluginName}：链路[{endpoint}]CA[{asdu.Ca}]总召唤被否定确认。", "IEC104插件");
                    }
                    break;
                case Iec104FrameHelper.TiClockSync:
                    break;
                default:
                    LogHelper.SysLogWrite("Iec104Plugin", "HandleAsdu", $"{PluginName}：链路[{endpoint}]收到不支持的TI[{asdu.Ti}]COT[{asdu.Cot}]，跳过。", "IEC104插件");
                    break;
            }
        }

        #endregion

        #region 数据解码与发布

        /// <summary>
        /// 监视方向数据发布:按CA定位设备绑定→按IOA匹配点表→IV无效品质丢弃→
        /// 套用参数模板与公式构建DeviceData发布协议解析(COT不限:20响应站召唤与3突发同路)
        /// </summary>
        private void PublishMonitorData(string endpoint, Iec104Asdu asdu)
        {
            try
            {
                Iec104DeviceBinding? binding;
                lock (_bindingLock)
                {
                    binding = _endpointMap.TryGetValue(endpoint, out var list)
                        ? list.Find(b => b.Ca == asdu.Ca)
                        : null;
                }
                if (binding == null)
                {
                    LogHelper.SysLogWrite("Iec104Plugin", "PublishMonitorData", $"{PluginName}：链路[{endpoint}]公共地址CA[{asdu.Ca}]未匹配任何设备，跳过。", "IEC104插件");
                    return;
                }

                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in asdu.Items)
                {
                    // IV品质无效值不入库(方案§3:QDS→Quality非0;NT/SB/BL/OV暂按可用值放行)
                    if ((item.Quality & 0x80) != 0)
                    {
                        LogHelper.SysLogWrite("Iec104Plugin", "PublishMonitorData", $"{PluginName}：链路[{endpoint}]CA[{asdu.Ca}]IOA[{item.Ioa}]品质IV无效(0x{item.Quality:X2})，丢弃。", "IEC104插件");
                        continue;
                    }
                    var point = binding.Points.Find(t => t.ParamAddr == item.Ioa);
                    if (point == null) continue;
                    values[point.ParamCode] = item.Value;
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
            catch (Exception ex) { LogHelper.ErrorLogWrite("Iec104Plugin", "PublishMonitorData", ex.ToString(), "IEC104插件"); }
        }

        /// <summary>
        /// 按设备参数模板构建DeviceData(套用ParamFormula公式,按最大/最小合法值标记IsAlarm;
        /// 与Modbus插件同口径,平台侧处理零差异)
        /// </summary>
        private DeviceData? BuildDeviceData(Iec104DeviceBinding binding, Dictionary<string, string> values)
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
                List<Iec104DeviceBinding> bindings;
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
                LogHelper.SysLogWrite("Iec104Plugin", "PublishRunStateByEndpoint", $"{PluginName}：{endpoint}{(state == 2 ? "上线" : "离线")}，涉及{datalist.Count}台设备。", "IEC104插件");
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Iec104Plugin", "PublishRunStateByEndpoint", ex.ToString(), "IEC104插件"); }
        }

        #endregion

        #region 定时器(t1~t3/周期总召唤/控制超时)

        /// <summary>
        /// 秒级巡检:t1等确认超时断链→t2欠账发S帧→t3空闲发TESTFR→周期总召唤→控制超时
        /// (t0连接建立超时由通道池连接与退避承担——方案§2)
        /// </summary>
        private void CheckTimers(object? _)
        {
            try
            {
                var cfg = _config;
                var pool = _clientPool;
                if (cfg == null || pool == null) return;
                List<string> endpoints;
                lock (_states) { endpoints = _states.Keys.ToList(); }
                var now = DateTime.Now;
                foreach (var endpoint in endpoints)
                {
                    if (!pool.IsOnline(endpoint)) continue;
                    var state = GetState(endpoint);
                    PendingControl? timeoutcontrol = null;
                    lock (state)
                    {
                        // t1:STARTDT/TESTFR/I帧等确认超时→断链重连(方案§4.4)
                        if (state.StartDtSent != null && (now - state.StartDtSent.Value).TotalSeconds > cfg.T1Seconds)
                        {
                            ForceReconnect(endpoint, "STARTDT等确认超时(t1)");
                            continue;
                        }
                        if (state.TestFrSent != null && (now - state.TestFrSent.Value).TotalSeconds > cfg.T1Seconds)
                        {
                            ForceReconnect(endpoint, "TESTFR等确认超时(t1)");
                            continue;
                        }
                        if (state.PendingI.Count > 0 && (now - state.PendingI.Peek().Sent).TotalSeconds > cfg.T1Seconds)
                        {
                            ForceReconnect(endpoint, "I帧等确认超时(t1)");
                            continue;
                        }
                        // t2:接收欠账超时发S帧确认
                        if (state.FirstUnackedRx != null && (now - state.FirstUnackedRx.Value).TotalSeconds > cfg.T2Seconds)
                        {
                            SendSFrame(endpoint, state);
                        }
                        // t3:双向空闲发TESTFR_act探活
                        if (state.TestFrSent == null
                            && (now - state.LastRx).TotalSeconds > cfg.T3Seconds
                            && (now - state.LastTx).TotalSeconds > cfg.T3Seconds)
                        {
                            state.TestFrSent = now;
                            SendRaw(endpoint, state, Iec104FrameHelper.BuildUFrame(Iec104FrameHelper.TestFrAct));
                        }
                        // 周期总召唤(0=仅连接时)
                        if (state.Started && cfg.GiCycleMinutes > 0
                            && (now - state.LastGi).TotalMinutes > cfg.GiCycleMinutes)
                        {
                            SendInterrogation(endpoint, state);
                        }
                        // 控制超时
                        if (state.Control != null && (now - state.Control.StartTime).TotalSeconds > cfg.CmdTimeoutSeconds)
                        {
                            timeoutcontrol = state.Control;
                            state.Control = null;
                        }
                    }
                    if (timeoutcontrol != null)
                    {
                        _ = PublishControlResultAsync(timeoutcontrol.CommandId, timeoutcontrol.DeviceId, timeoutcontrol.DeviceName, false, "控制指令超时");
                    }
                }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Iec104Plugin", "CheckTimers", ex.ToString(), "IEC104插件"); }
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
            catch (Exception ex) { LogHelper.ErrorLogWrite("Iec104Plugin", "SendMessageAsync", ex.ToString(), "IEC104插件"); }
        }

        #endregion

        #region 接收主程序消息

        /// <summary>
        /// 接收主程序消息入口(心跳/设备控制/配置更新)
        /// </summary>
        public async Task ReceiveMessageAsync(PluginMessage mess)
        {
            switch (mess.MessageType)
            {
                case PluginMessageEnum.心跳:
                    LogHelper.SysLogWrite("Iec104Plugin", "ReceiveMessageAsync", $"{PluginName}：收到心跳。", "IEC104插件");
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
        /// 配置更新自重启(设备/点表/链路拓扑变更后全量重建;PluginStart无防重入护栏须闸门串行,
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
                    LogHelper.SysLogWrite("Iec104Plugin", "RestartForConfigUpdateAsync", $"{PluginName}：收到配置更新，重建链路拓扑。", "IEC104插件");
                    await PluginStop();
                    await PluginStart(_config?.ToJson() ?? "");
                }
            }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Iec104Plugin", "RestartForConfigUpdateAsync", ex.ToString(), "IEC104插件"); }
            finally { _restartGate.Release(); }
        }

        #endregion

        #region 遥控/设定值下发

        /// <summary>
        /// 处理写点位控制(netiec104write:按参数编码定位点表,TI映射命令类型,
        /// 默认SBO选择后执行——方案§4.6;单链路同时最多一条挂起控制)
        /// </summary>
        private async Task HandleDeviceControlAsync(string? messagejson)
        {
            if (messagejson.IsZxxNullOrEmpty())
            {
                LogHelper.SysLogWrite("Iec104Plugin", "HandleDeviceControlAsync", $"{PluginName}：设备控制消息为空。", "IEC104插件");
                return;
            }
            Iec104ControlCommand? command;
            try { command = messagejson.ToObject<Iec104ControlCommand>(); }
            catch (Exception ex) { LogHelper.ErrorLogWrite("Iec104Plugin", "HandleDeviceControlAsync", ex.ToString(), "IEC104插件"); return; }
            if (command == null || command.ConContent.IsZxxNullOrEmpty())
            {
                LogHelper.SysLogWrite("Iec104Plugin", "HandleDeviceControlAsync", $"{PluginName}：设备控制消息格式无效。", "IEC104插件");
                return;
            }
            if (!string.Equals(command.ClassName?.Trim(), "netiec104write", StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.SysLogWrite("Iec104Plugin", "HandleDeviceControlAsync", $"{PluginName}：不支持的控制类型[{command.ClassName}]，CommandId={command.CommandId}。", "IEC104插件");
                return;
            }
            NetIec104Write? model;
            try { model = command.ConContent.ToObject<NetIec104Write>(); }
            catch { LogHelper.SysLogWrite("Iec104Plugin", "HandleDeviceControlAsync", $"{PluginName}：NetIec104Write解析失败。", "IEC104插件"); return; }
            if (model == null || model.ParamCode.IsZxxNullOrEmpty()) return;

            foreach (var deviceid in command.DeviceIds.Distinct())
            {
                Iec104DeviceBinding? binding;
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
                if (!TryBuildCommandAsdu(binding.Ca, point, model.ParamValue, out byte cmdti, out var selectasdu, out var executeasdu))
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, binding.Device.DeviceName, false, "值编码失败或点表TI不支持下发");
                    continue;
                }

                var state = GetState(binding.Endpoint);
                string? failreason = null;
                lock (state)
                {
                    if (!state.Started) failreason = "链路未就绪(STARTDT未完成)";
                    else if (state.Control != null) failreason = "上一条控制未完成";
                    else
                    {
                        bool sbo = _config!.UseSelectBeforeOperate;
                        var pending = new PendingControl
                        {
                            CommandId = command.CommandId,
                            DeviceId = deviceid,
                            DeviceName = binding.Device.DeviceName,
                            Ti = cmdti,
                            Ioa = point.ParamAddr,
                            SelectPhase = sbo,
                            ExecuteAsdu = executeasdu,
                            StartTime = DateTime.Now
                        };
                        if (!SendIFrame(binding.Endpoint, state, sbo ? selectasdu : executeasdu))
                        {
                            failreason = "发送窗口已满或链路不可写";
                        }
                        else
                        {
                            state.Control = pending;
                        }
                    }
                }
                if (failreason != null)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, binding.Device.DeviceName, false, failreason);
                    continue;
                }
                LogHelper.SysLogWrite("Iec104Plugin", "HandleDeviceControlAsync", $"{PluginName}：控制入队，设备[{deviceid}]参数[{model.ParamCode}]值[{model.ParamValue}]TI[{cmdti}]。", "IEC104插件");
            }
        }

        /// <summary>
        /// 按点表TI构建命令ASDU对(选择帧+执行帧):1/30→单点命令45,3/31→双点命令46,
        /// 9/11/13/34/36→短浮点设定值50
        /// </summary>
        private static bool TryBuildCommandAsdu(int ca, DeviceTypeParam point, string value,
            out byte cmdti, out byte[] selectasdu, out byte[] executeasdu)
        {
            cmdti = 0;
            selectasdu = Array.Empty<byte>();
            executeasdu = Array.Empty<byte>();
            int ioa = point.ParamAddr;
            switch ((byte)point.CollectFuncCode)
            {
                case Iec104FrameHelper.TiSinglePoint:
                case Iec104FrameHelper.TiSinglePointTime:
                    {
                        bool on = IsOnValue(value);
                        cmdti = Iec104FrameHelper.TiSingleCommand;
                        selectasdu = Iec104FrameHelper.BuildSingleCommand(ca, ioa, on, true);
                        executeasdu = Iec104FrameHelper.BuildSingleCommand(ca, ioa, on, false);
                        return true;
                    }
                case Iec104FrameHelper.TiDoublePoint:
                case Iec104FrameHelper.TiDoublePointTime:
                    {
                        bool on = IsOnValue(value);
                        cmdti = Iec104FrameHelper.TiDoubleCommand;
                        selectasdu = Iec104FrameHelper.BuildDoubleCommand(ca, ioa, on, true);
                        executeasdu = Iec104FrameHelper.BuildDoubleCommand(ca, ioa, on, false);
                        return true;
                    }
                case Iec104FrameHelper.TiNormalized:
                case Iec104FrameHelper.TiScaled:
                case Iec104FrameHelper.TiFloat:
                case Iec104FrameHelper.TiNormalizedTime:
                case Iec104FrameHelper.TiFloatTime:
                    {
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return false;
                        cmdti = Iec104FrameHelper.TiSetpointFloat;
                        selectasdu = Iec104FrameHelper.BuildSetpointFloat(ca, ioa, f, true);
                        executeasdu = Iec104FrameHelper.BuildSetpointFloat(ca, ioa, f, false);
                        return true;
                    }
                default:
                    return false;
            }
        }

        /// <summary>
        /// 遥控值语义:非0数值/true即合(ON)
        /// </summary>
        private static bool IsOnValue(string value) =>
            value.Trim().ToLowerInvariant() is "1" or "true" or "on"
            || (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && d != 0);

        /// <summary>
        /// 命令镜像处理(SBO链推进——方案§4.6:选择确认→下发执行帧;执行确认→回执成功;
        /// 否定确认P/N=1→回执失败;执行阶段以激活确认即成功,激活终止到达时已无挂起,自然忽略,
        /// 避免子站不发actterm时控制悬死)
        /// </summary>
        private void HandleControlMirror(string endpoint, Iec104Asdu asdu)
        {
            var state = GetState(endpoint);
            PendingControl? finished = null;
            bool success = false;
            string message = "";
            lock (state)
            {
                var control = state.Control;
                if (control == null || control.Ti != asdu.Ti) return;
                var item = asdu.Items.Find(t => t.Ioa == control.Ioa);
                if (item == null) return;

                if (asdu.Negative)
                {
                    finished = control;
                    state.Control = null;
                    message = control.SelectPhase ? "子站否定选择确认" : "子站否定执行确认";
                }
                else if (asdu.Cot == Iec104FrameHelper.CotActivationCon)
                {
                    if (control.SelectPhase)
                    {
                        control.SelectPhase = false;
                        if (!SendIFrame(endpoint, state, control.ExecuteAsdu))
                        {
                            finished = control;
                            state.Control = null;
                            message = "执行帧发送失败";
                        }
                    }
                    else
                    {
                        finished = control;
                        state.Control = null;
                        success = true;
                        message = "控制成功";
                    }
                }
            }
            if (finished != null)
            {
                _ = PublishControlResultAsync(finished.CommandId, finished.DeviceId, finished.DeviceName, success, message);
            }
        }

        #endregion

        #region 控制结果发布

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
