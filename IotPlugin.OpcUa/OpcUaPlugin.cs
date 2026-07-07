using System.Collections.Concurrent;
using System.Globalization;
using CenBoCommon.Zxx;
using CenboEventBus;
using IotDriverCore;
using IotLog;
using IotModel;
using NewLife.Threading;
using Opc.Ua;
using Opc.Ua.Client;

namespace IotPlugin.OpcUa
{
    /// <summary>
    /// OPC UA采集插件(M3.6下半:OPCFoundation官方客户端栈,平台直连型;
    /// 一台设备=一个OPC UA服务器(opc.tcp://DeviceIp:DevicePort),点表NodeId取collect_node_id列;
    /// 双模式:订阅推送(Subscription+MonitoredItem,服务端变化上报)/批量轮询读(ReadValueId);
    /// 匿名/用户名两种认证(证书认证待后续);每设备独立会话循环+ReconnectBackoff退避重连;
    /// 写下发netopcuawrite:CollectWritable点位入每设备写队列由会话循环串行消费)
    /// </summary>
    public class OpcUaPlugin : ICenBoPlugin
    {
        private IEventBus<PluginEvent>? _eventBus;
        private OpcUaPluginConfig? _config;
        private TimerX? _heartTimer;
        private CancellationTokenSource? _cts;
        private ApplicationConfiguration? _appConfig;

        private readonly object _bindingLock = new();

        /// <summary>设备ID→绑定</summary>
        private Dictionary<int, OpcUaDeviceBinding> _deviceMap = new();

        /// <summary>
        /// OPC UA设备绑定(一台服务器一条会话)
        /// </summary>
        private class OpcUaDeviceBinding
        {
            public DeviceInfo Device = null!;
            public DeviceParamEntity? DeviceParam;
            public List<DeviceTypeParam> Points = new();
            public volatile bool Online;

            /// <summary>待写请求队列(Session实例非线程安全,写必须由本设备会话循环串行消费)</summary>
            public readonly ConcurrentQueue<OpcUaWriteRequest> WriteQueue = new();
        }

        public void PluginInit(IEventBus<PluginEvent> eventBus) => _eventBus = eventBus;

        public string PluginName => "OpcUaPlugin";
        public string PluginType => "OPCUA采集插件";
        public string PluginDesc => "OPC UA订阅/批量读采集(基于IotDriverCore驱动框架,OPCFoundation官方栈)";
        public string PluginVersion => "1.0.0";
        public string PluginGuid => "f2a8b4c0d6e7951fa8b29cad3456f789";
        public string PluginModelPath => "";

        #region 启动/停止

        /// <summary>
        /// 启动插件:校验配置→构建应用配置(证书策略)→加载绑定→每设备拉起会话循环→启动心跳
        /// </summary>
        public async Task<bool> PluginStart(string _PluginConfig)
        {
            try
            {
                _config = OpcUaPluginConfig.Current;
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
                if (_config.CollectCycleMs <= 0)
                {
                    LogHelper.Info($"{PluginName}：配置参数存在非正数，插件启动失败。");
                    return false;
                }
                if (!RefreshBindings(out var err))
                {
                    LogHelper.Info($"{PluginName}：初始化设备失败，{err}");
                    return false;
                }

                _appConfig = await BuildApplicationConfigurationAsync();

                _cts = new CancellationTokenSource();
                List<OpcUaDeviceBinding> bindings;
                lock (_bindingLock) { bindings = _deviceMap.Values.ToList(); }
                foreach (var binding in bindings)
                {
                    var b = binding;
                    _ = Task.Run(() => RunDeviceLoopAsync(b, _cts.Token));
                }

                _heartTimer?.Dispose();
                _heartTimer = new TimerX(HeartBeatDown, null, 5_000, _config.HeartSecond * 1_000);
                LogHelper.Info($"{PluginName}：插件启动成功，{bindings.Count}台服务器。");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return false;
            }
        }

        /// <summary>
        /// 停止插件:销毁心跳→取消所有会话循环→清空绑定
        /// </summary>
        public async Task<bool> PluginStop()
        {
            try
            {
                _heartTimer?.Dispose();
                _cts?.Cancel();
                _cts = null;
                lock (_bindingLock) { _deviceMap = new Dictionary<int, OpcUaDeviceBinding>(); }
            }
            catch (Exception ex) { LogHelper.Error(ex); }
            return true;
        }

        /// <summary>
        /// 构建OPC UA客户端应用配置(自动生成自签名客户端证书;
        /// AutoAcceptUntrustedCertificates开启时接受不受信服务器证书)
        /// </summary>
        private async Task<ApplicationConfiguration> BuildApplicationConfigurationAsync()
        {
            var config = new ApplicationConfiguration
            {
                ApplicationName = "ZxxIotOpcUaPlugin",
                ApplicationUri = "urn:ZxxIot:OpcUaPlugin",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "Config/OpcUaPki/own",
                        SubjectName = "CN=ZxxIotOpcUaPlugin"
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "Config/OpcUaPki/issuer"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "Config/OpcUaPki/trusted"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = "Config/OpcUaPki/rejected"
                    },
                    AutoAcceptUntrustedCertificates = _config!.AutoAcceptUntrustedCertificates
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = _config.SessionTimeoutMs },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = _config.SessionTimeoutMs }
            };
            await config.Validate(ApplicationType.Client);
            if (_config.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (_, e) =>
                {
                    if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted) e.Accept = true;
                };
            }
            return config;
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
        /// 从数据库加载服务器绑定与点表并原子替换内存映射(点表条件:collect_node_id非空)
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
                .Where(t => typecodes.Contains(t.DeviceTypeCode) && !t.CollectNodeId.IsZxxNullOrEmpty())
                .GroupBy(t => t.DeviceTypeCode)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var deviceids = devices.Select(t => t.DeviceId).ToList();
            var devparams = DeviceParamDAO.Instance.GetListBy(t => deviceids.Contains(t.DeviceId));

            var devicemap = new Dictionary<int, OpcUaDeviceBinding>();
            foreach (var device in devices)
            {
                if (device.DeviceIp.IsZxxNullOrEmpty() || device.DevicePort <= 0)
                {
                    LogHelper.Info($"{PluginName}：设备[{device.DeviceId}({device.DeviceName})]未配置IP或端口，跳过。");
                    continue;
                }
                if (!pointmap.TryGetValue(device.DeviceTypeCode, out var points) || !points.IsZxxAny())
                {
                    LogHelper.Info($"{PluginName}：类型[{device.DeviceTypeCode}]无NodeId点表配置，设备[{device.DeviceId}]跳过。");
                    continue;
                }
                devicemap[device.DeviceId] = new OpcUaDeviceBinding
                {
                    Device = device,
                    DeviceParam = devparams?.Find(t => t.DeviceId == device.DeviceId),
                    Points = points
                };
            }
            if (!devicemap.Any())
            {
                error = "未构建任何有效服务器绑定。";
                return false;
            }

            lock (_bindingLock) { _deviceMap = devicemap; }
            LogHelper.Info($"{PluginName}：服务器映射初始化完成，{devicemap.Count}台。");
            return true;
        }

        #endregion

        #region 会话循环

        /// <summary>
        /// 单台服务器会话主循环:建会话(退避重连)→订阅或轮询→断线发布离线并重连
        /// </summary>
        private async Task RunDeviceLoopAsync(OpcUaDeviceBinding binding, CancellationToken token)
        {
            var backoff = new ReconnectBackoff();
            string url = $"opc.tcp://{binding.Device.DeviceIp}:{binding.Device.DevicePort}";
            while (!token.IsCancellationRequested)
            {
                Session? session = null;
                try
                {
                    var endpoint = CoreClientUtils.SelectEndpoint(_appConfig, url, useSecurity: false);
                    var identity = _config!.UserName.IsZxxNullOrEmpty()
                        ? new UserIdentity(new AnonymousIdentityToken())
                        : new UserIdentity(_config.UserName, _config.Password);
                    session = await Session.Create(_appConfig, new ConfiguredEndpoint(null, endpoint,
                        EndpointConfiguration.Create(_appConfig)), false, PluginName,
                        (uint)_config.SessionTimeoutMs, identity, null);

                    backoff.Reset();
                    binding.Online = true;
                    LogHelper.Info($"{PluginName}：服务器[{binding.Device.DeviceId}({url})]会话建立。");
                    PublishRunState(binding, 2);

                    if (_config.CollectMode == 2)
                    {
                        await RunPollLoopAsync(binding, session, token);
                    }
                    else
                    {
                        RunSubscription(binding, session);
                        // 保活循环500ms切片,顺带消费写队列(写延迟上限500ms)
                        while (!token.IsCancellationRequested && session.Connected)
                        {
                            DrainWriteQueue(binding, session);
                            await Task.Delay(500, token);
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    LogHelper.Info($"{PluginName}：服务器[{binding.Device.DeviceId}]通信异常，{ex.Message}");
                }
                finally
                {
                    if (binding.Online)
                    {
                        binding.Online = false;
                        LogHelper.Info($"{PluginName}：服务器[{binding.Device.DeviceId}]会话断开。");
                        PublishRunState(binding, 0);
                    }
                    try { session?.Close(); session?.Dispose(); } catch { }
                    FailPendingWrites(binding, "OPC UA会话断开，写请求未执行");
                }
                if (token.IsCancellationRequested) break;
                try { await Task.Delay(backoff.NextDelayMs(), token); }
                catch (OperationCanceledException) { break; }
            }
        }

        /// <summary>
        /// 订阅模式:全部点位挂MonitoredItem,服务端变化推送(天然自带变化上报语义)
        /// </summary>
        private void RunSubscription(OpcUaDeviceBinding binding, Session session)
        {
            var subscription = new Subscription(session.DefaultSubscription)
            {
                PublishingInterval = _config!.CollectCycleMs,
                PublishingEnabled = true
            };
            foreach (var point in binding.Points)
            {
                var item = new MonitoredItem(subscription.DefaultItem)
                {
                    DisplayName = point.ParamCode,
                    StartNodeId = new NodeId(point.CollectNodeId),
                    AttributeId = Attributes.Value,
                    SamplingInterval = _config.CollectCycleMs
                };
                var code = point.ParamCode;
                item.Notification += (monitoreditem, args) =>
                {
                    try
                    {
                        foreach (var value in monitoreditem.DequeueValues())
                        {
                            if (value.Value == null) continue;
                            PublishValues(binding, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                [code] = Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? ""
                            });
                        }
                    }
                    catch (Exception ex) { LogHelper.Error(ex); }
                };
                subscription.AddItem(item);
            }
            session.AddSubscription(subscription);
            subscription.Create();
            LogHelper.Info($"{PluginName}：服务器[{binding.Device.DeviceId}]订阅建立，{binding.Points.Count}个监控项。");
        }

        /// <summary>
        /// 轮询模式:按周期批量读全部NodeId(一次Read多NodeId,§6.7批量读)
        /// </summary>
        private async Task RunPollLoopAsync(OpcUaDeviceBinding binding, Session session, CancellationToken token)
        {
            var nodes = new ReadValueIdCollection(binding.Points.Select(t => new ReadValueId
            {
                NodeId = new NodeId(t.CollectNodeId),
                AttributeId = Attributes.Value
            }));
            while (!token.IsCancellationRequested && session.Connected)
            {
                DrainWriteQueue(binding, session);
                session.Read(null, 0, TimestampsToReturn.Neither, nodes,
                    out DataValueCollection results, out _);
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < results.Count && i < binding.Points.Count; i++)
                {
                    if (!StatusCode.IsGood(results[i].StatusCode) || results[i].Value == null) continue;
                    values[binding.Points[i].ParamCode] = Convert.ToString(results[i].Value, CultureInfo.InvariantCulture) ?? "";
                }
                if (values.Any()) PublishValues(binding, values);
                // 采集周期500ms切片,顺带消费写队列(写延迟不受周期长短拖累)
                int remaining = _config!.CollectCycleMs;
                while (remaining > 0 && !token.IsCancellationRequested && session.Connected)
                {
                    int slice = Math.Min(500, remaining);
                    await Task.Delay(slice, token);
                    remaining -= slice;
                    DrainWriteQueue(binding, session);
                }
            }
        }

        #endregion

        #region 写下发

        /// <summary>
        /// 冲刷写队列(由持有会话的循环线程调用——Session非线程安全;单条失败只回执不断连)
        /// </summary>
        private void DrainWriteQueue(OpcUaDeviceBinding binding, Session session)
        {
            while (session.Connected && binding.WriteQueue.TryDequeue(out var request))
            {
                bool success = false;
                string message;
                try
                {
                    object converted = ConvertWriteValue(session, request.Point, request.ParamValue);
                    var writevalue = new WriteValue
                    {
                        NodeId = new NodeId(request.Point.CollectNodeId),
                        AttributeId = Attributes.Value,
                        Value = new DataValue(new Variant(converted))
                    };
                    session.Write(null, new WriteValueCollection { writevalue },
                        out StatusCodeCollection results, out _);
                    if (results.Count > 0 && StatusCode.IsGood(results[0]))
                    {
                        success = true;
                        message = "写入成功";
                        LogHelper.Info($"{PluginName}：服务器[{binding.Device.DeviceId}]写点位[{request.Point.ParamCode}]值[{request.ParamValue}]成功。");
                    }
                    else
                    {
                        message = $"服务器拒绝写入：{(results.Count > 0 ? results[0].ToString() : "无状态码")}";
                        LogHelper.Info($"{PluginName}：服务器[{binding.Device.DeviceId}]写点位[{request.Point.ParamCode}]被拒，{message}");
                    }
                }
                catch (Exception ex)
                {
                    message = $"写入异常：{ex.Message}";
                    LogHelper.Info($"{PluginName}：服务器[{binding.Device.DeviceId}]写点位[{request.Point.ParamCode}]失败，{ex.Message}");
                }
                _ = PublishControlResultAsync(request.CommandId, binding.Device.DeviceId, binding.Device.DeviceName, success, message);
            }
        }

        /// <summary>
        /// 工程值字符串按点表数据类型转换为CLR值(未配置类型时读服务器当前值按其运行时类型转换,
        /// 避免Variant类型不匹配被BadTypeMismatch拒绝;转换失败上抛由调用方回执)
        /// </summary>
        private static object ConvertWriteValue(Session session, DeviceTypeParam point, string value)
        {
            var type = (point.CollectDataType ?? "").Trim().ToLowerInvariant();
            value = value?.Trim() ?? "";
            switch (type)
            {
                case "bool":
                case "bit":
                    return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
                case "byte":
                    return byte.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "int16":
                    return short.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "uint16":
                    return ushort.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "int32":
                    return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "uint32":
                    return uint.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                case "float32":
                    return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                case "float64":
                case "double":
                    return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
                case "string":
                    return value;
                default:
                    var current = session.ReadValue(new NodeId(point.CollectNodeId));
                    if (current?.Value == null) return value;
                    return Convert.ChangeType(value, current.Value.GetType(), CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// 断线清空写队列并逐条发失败回执
        /// </summary>
        private void FailPendingWrites(OpcUaDeviceBinding binding, string reason)
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
        /// 按设备参数模板构建DeviceData并发布协议解析消息
        /// </summary>
        private void PublishValues(OpcUaDeviceBinding binding, Dictionary<string, string> values)
        {
            try
            {
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
        /// 按设备参数模板构建DeviceData(套用ParamFormula公式,按最大/最小合法值标记IsAlarm)
        /// </summary>
        private DeviceData? BuildDeviceData(OpcUaDeviceBinding binding, Dictionary<string, string> values)
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
        /// 发布单台服务器运行状态(2在线/0离线)
        /// </summary>
        private void PublishRunState(OpcUaDeviceBinding binding, int state)
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
        /// 处理写点位控制(netopcuawrite:按参数编码定位点表NodeId,入每设备写队列由会话循环串行消费——
        /// Session实例非线程安全,不可在消息线程直接写)
        /// </summary>
        private async Task HandleDeviceControlAsync(string? messagejson)
        {
            if (messagejson.IsZxxNullOrEmpty())
            {
                LogHelper.Info($"{PluginName}：设备控制消息为空。");
                return;
            }
            OpcUaControlCommand? command;
            try { command = messagejson.ToObject<OpcUaControlCommand>(); }
            catch (Exception ex) { LogHelper.Error(ex); return; }
            if (command == null || command.ConContent.IsZxxNullOrEmpty())
            {
                LogHelper.Info($"{PluginName}：设备控制消息格式无效。");
                return;
            }
            if (!string.Equals(command.ClassName?.Trim(), "netopcuawrite", StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.Info($"{PluginName}：不支持的控制类型[{command.ClassName}]，CommandId={command.CommandId}。");
                return;
            }
            NetOpcUaWrite? model;
            try { model = command.ConContent.ToObject<NetOpcUaWrite>(); }
            catch { LogHelper.Info($"{PluginName}：NetOpcUaWrite解析失败。"); return; }
            if (model == null || model.ParamCode.IsZxxNullOrEmpty()) return;

            foreach (var deviceid in command.DeviceIds.Distinct())
            {
                OpcUaDeviceBinding? binding;
                lock (_bindingLock) { _deviceMap.TryGetValue(deviceid, out binding); }
                if (binding == null)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, "", false, "未找到设备绑定信息");
                    continue;
                }
                var point = binding.Points.Find(t => string.Equals(t.ParamCode, model.ParamCode, StringComparison.OrdinalIgnoreCase));
                if (point == null || !point.CollectWritable || point.CollectNodeId.IsZxxNullOrEmpty())
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, binding.Device.DeviceName, false, "点位不存在、不可写或未配置NodeId");
                    continue;
                }
                if (!binding.Online)
                {
                    await PublishControlResultAsync(command.CommandId, deviceid, binding.Device.DeviceName, false, "服务器离线，写请求未受理");
                    continue;
                }
                binding.WriteQueue.Enqueue(new OpcUaWriteRequest
                {
                    CommandId = command.CommandId,
                    Point = point,
                    ParamValue = model.ParamValue
                });
                LogHelper.Info($"{PluginName}：写点位入队，设备[{deviceid}]参数[{model.ParamCode}]值[{model.ParamValue}]。");
            }
        }

        #endregion
    }
}
