using CenboEventBus;
using CenBoCommon.Zxx;
using IotLog;
using MQTTnet;
using MQTTnet.Protocol;
using Quartz;
using System.Buffers;
using System.Text;
using System.Threading;
using IotModel;
using IotWebApi.Services.Mqtt;

namespace IotWebApi.Services.Jobs
{
    /// <summary>
    /// MQTT客户端状态检查任务
    /// </summary>
    [DisallowConcurrentExecution] // 禁止并发执行
    public class MqttClientJob : BaseJob
    {
        private string mainTopic = "";
        private MqttClientOptions? clientOptions;
        private AdminMqttparam? MqttParam;
        private string mqttname = "";
        /// <summary>
        /// MQTT客户端重连串行锁:断线重连与Quartz巡检重连共用同一入口,
        /// 保证任意时刻只有一条重连在跑,不会建出多个并存客户端导致上行重复。
        /// </summary>
        private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
        /// <summary>
        /// 执行任务
        /// </summary>
        protected override async Task<string> ExecuteJob(IJobExecutionContext context)
        {
            try
            {
                mqttname = "IotWebApi_Main";
                // 检查MQTT客户端状态
                bool clientStatus = await CheckMqttClientStatus();
                return $"MQTT客户端状态: {(clientStatus ? "正常" : "异常")}";
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, ex.ToString(), mqttname);
                throw; // 将异常抛出，由基类记录错误日志
            }
        }

        /// <summary>
        /// 检查MQTT客户端状态
        /// </summary>
        private async Task<bool> CheckMqttClientStatus()
        {
            try
            {
                bool isConnected = MqttClientService._mqttClient != null && MqttClientService._mqttClient.IsConnected;
                if (!isConnected)
                {
                    // 尝试初始化客户端
                    await TryInitializeClient();
                }
                isConnected = MqttClientService._mqttClient != null && MqttClientService._mqttClient.IsConnected;

                return isConnected;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"检查MQTT客户端状态失败: {ex.Message}", mqttname);
                return false;
            }
        }

        /// <summary>
        /// 尝试初始化MQTT客户端
        /// </summary>
        private async Task<bool> TryInitializeClient()
        {
            // 串行化:断线重连与巡检重连都经此入口,任意时刻只放行一条,杜绝多客户端并存
            await _initLock.WaitAsync();
            try
            {
                // 幂等:若已有其它重连路径把连接建好,直接返回,不重复建客户端
                if (MqttClientService._mqttClient != null && MqttClientService._mqttClient.IsConnected)
                    return true;

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "尝试初始化MQTT客户端", mqttname);

                await Task.Delay(1000);
                MqttParam = AdminMqttparamDAO.Instance.GetOneBy(t => t.Id > 0);
                if (MqttParam == null)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "无法初始化MQTT客户端: MqttParam为空", mqttname);
                    return false;
                }

                // 释放旧客户端(断开的残留连接),防止新旧客户端并存造成上行消息重复入库
                var old = MqttClientService._mqttClient;
                if (old != null)
                {
                    MqttClientService._mqttClient = null;
                    try { if (old.IsConnected) await old.DisconnectAsync(); } catch { }
                    try { old.Dispose(); } catch { }
                }

                // 创建客户端选项
                var optionsBuilder = new MqttClientOptionsBuilder()
                 .WithTcpServer(MqttParam.MqttServer, MqttParam.MqttClientPort)
                 .WithCredentials(MqttParam.MqttUser, MqttParam.MqttPass)
                 .WithClientId(mqttname)  // 固定ClientId:持久会话(CleanSession=false)依赖稳定标识;随机雪花ID会让会话永不复用并在broker端堆积僵尸会话,重连时broker也无法用同名连接顶替旧连接
                 .WithCleanSession(false) //建议保持会话
                 .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                 .WithKeepAlivePeriod(TimeSpan.FromSeconds(15))  // 心跳间隔时间
                 .WithTimeout(TimeSpan.FromSeconds(10))  // 连接超时时间
                 .WithTlsOptions(new MqttClientTlsOptions
                 {
                     UseTls = false  // 是否使用 tls加密
                 });
                clientOptions = optionsBuilder.Build();

                // 创建客户端实例并注册事件,先备妥再挂到静态字段
                var factory = new MqttClientFactory();
                var client = factory.CreateMqttClient();
                client.ConnectedAsync += ConnectedHandler;
                client.DisconnectedAsync += DisconnectedHandler;
                client.ApplicationMessageReceivedAsync += MessageReceivedHandler;
                MqttClientService._mqttClient = client;

                // 不再fire-and-forget:等待连接结果并记录异常,失败留待下次巡检/断线重连
                try
                {
                    await client.ConnectAsync(clientOptions);
                }
                catch (Exception cex)
                {
                    LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"MQTT客户端连接失败: {cex.Message}", mqttname);
                    return false;
                }

                LogHelper.SysLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, "MQTT客户端初始化成功", mqttname);
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite(ClassHelper.ClassName, ClassHelper.MethodName, $"初始化MQTT客户端失败: {ex.Message}", mqttname);
                return false;
            }
            finally
            {
                _initLock.Release();
            }
        }

        #region MQTT客户端事件处理

        /// <summary>
        /// 连接成功事件处理
        /// </summary>
        private async Task ConnectedHandler(MqttClientConnectedEventArgs args)
        {
            var _mainTopic = mainTopic = MqttParam.MqttSubTopicWebApi;
            if (!_mainTopic.Contains("+")) _mainTopic = _mainTopic + "/+";
            // 订阅消息主题
            // MqttQualityOfServiceLevel: （QoS）:  0 最多一次，接收者不确认收到消息，并且消息不被发送者存储和重新发送提供与底层 TCP 协议相同的保证。
            // 1: 保证一条消息至少有一次会传递给接收方。发送方存储消息，直到它从接收方收到确认收到消息的数据包。一条消息可以多次发送或传递。
            // 2: 保证每条消息仅由预期的收件人接收一次。级别2是最安全和最慢的服务质量级别，保证由发送方和接收方之间的至少两个请求/响应（四次握手）。
            await MqttClientService._mqttClient.SubscribeAsync(_mainTopic, MqttQualityOfServiceLevel.AtLeastOnce);
            LogHelper.SysLogWrite("MqttClientJob", "ConnectedHandler", "webapi服务端的连接成功并订阅", mqttname);
        }

        /// <summary>
        /// 连接断开事件处理
        /// </summary>
        private Task DisconnectedHandler(MqttClientDisconnectedEventArgs args)
        {
            LogHelper.SysLogWrite("MqttClientJob", "DisconnectedHandler", $"MQTT客户端连接断开，原因: {args.Reason}", mqttname);
            // 脱离断开事件的回调栈后再重连:避免在自身断开回调内Dispose自己的客户端;
            // 重连统一走TryInitializeClient(信号量串行+幂等),与巡检重连不再打架。
            _ = Task.Run(async () =>
            {
                await Task.Delay(20 * 1000);
                await TryInitializeClient();
            });
            return Task.CompletedTask;
        }

        /// <summary>
        /// 接收消息事件处理(M0遗留重写:MQTT上行统一并入插件上行管道——
        /// 载荷契约①PluginMessage{MessageType,MessageJson}原样路由,
        /// 契约②裸List&lt;DeviceData&gt;默认按协议解析包装;
        /// 经PluginEvent总线进DataPointIngestService,与插件链路同构)
        /// </summary>
        private Task MessageReceivedHandler(MqttApplicationMessageReceivedEventArgs args)
        {
            if (!args.ApplicationMessage.Topic.Contains(mainTopic)) return Task.CompletedTask;
            var buffer = args.ApplicationMessage.Payload.ToArray();
            if (buffer == null || buffer.Length == 0) return Task.CompletedTask;

            string strdata = Encoding.UTF8.GetString(buffer);
            if (AppSetting.GetConfig("MqttConfig:LogOpen").ToLower() == "true")
                LogHelper.SysLogWrite("MqttClientJob", "MessageReceivedHandler", $"Mqtt接收数据：{strdata}", mqttname);

            try
            {
                var bus = MqttClientService.EventBus;
                if (bus == null)
                {
                    LogHelper.SysLogWrite("MqttClientJob", "MessageReceivedHandler", "事件总线未就绪，MQTT上行消息已丢弃。", mqttname);
                    return Task.CompletedTask;
                }
                PluginMessage? message = null;
                try
                {
                    var wrapped = strdata.ToObject<PluginMessage>();
                    if (wrapped != null && !wrapped.MessageJson.IsZxxNullOrEmpty()) message = wrapped;
                }
                catch { }
                if (message == null)
                {
                    try
                    {
                        var datalist = strdata.ToObject<List<DeviceData>>();
                        if (datalist.IsZxxAny() && datalist.Exists(t => t.DeviceId > 0))
                        {
                            message = new PluginMessage
                            {
                                MessageType = PluginMessageEnum.协议解析,
                                MessageJson = strdata
                            };
                        }
                    }
                    catch { }
                }
                if (message == null)
                {
                    // 契约③兜底:非JSON载荷按产品挂JS脚本解码(§6.5,deviceKey=topic末段)
                    var segments = args.ApplicationMessage.Topic.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    string devicekey = segments.Length > 0 ? segments[^1] : "";
                    var scriptdata = MqttClientService.ScriptService?.DecodePayload(devicekey, buffer);
                    if (scriptdata.IsZxxAny())
                    {
                        message = new PluginMessage
                        {
                            MessageType = PluginMessageEnum.协议解析,
                            MessageJson = scriptdata.ToJson()
                        };
                    }
                }
                if (message == null)
                {
                    LogHelper.SysLogWrite("MqttClientJob", "MessageReceivedHandler",
                        $"无法识别的MQTT上行载荷，已忽略：{strdata[..Math.Min(200, strdata.Length)]}", mqttname);
                    return Task.CompletedTask;
                }
                bus.Publish(new PluginEvent(MqttClientService.MqttPluginGuid, message));
            }
            catch (Exception ex)
            {
                LogHelper.SysLogWrite("MqttClientJob", "MessageReceivedHandler", ex.ToString(), mqttname);
            }
            return Task.CompletedTask;
        }

        #endregion
    }
}