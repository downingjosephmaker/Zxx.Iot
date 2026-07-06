using CenBoCommon.Zxx;
using IotLog;
using MQTTnet;
using MQTTnet.Protocol;
using System.Buffers;
using System.Text;
using IotModel;

namespace IotWebApi.Services.Mqtt
{
    public class MqttClientHostService : IHostedService
    {
        private string mainTopic = "";
        private MqttClientOptions? clientOptions;
        private AdminMqttparam? MqttParam;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1000);
            MqttConnect();
        }

        private void MqttConnect()
        {
            bool iscontinue = false;
            MqttParam = AdminMqttparamDAO.Instance.GetOneBy(t => t.Id > 0);
            if (MqttParam != null)
            {
                var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(MqttParam.MqttServer, MqttParam.MqttClientPort)
                .WithCredentials(MqttParam.MqttUser, MqttParam.MqttPass)
                .WithClientId($"IotWebApi_{SnowModel.Instance.NewId()}")
                .WithCleanSession(false) //建议保持会话
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V311)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(15))
                .WithTimeout(TimeSpan.FromSeconds(10))
                .WithTlsOptions(new MqttClientTlsOptions
                {
                    UseTls = false  // 是否使用 tls加密
                });
                clientOptions = optionsBuilder.Build();

                MqttClientService._mqttClient = new MqttClientFactory().CreateMqttClient();
                MqttClientService._mqttClient.ConnectedAsync += MqttConnectedAsync; // 客户端连接成功事件
                MqttClientService._mqttClient.DisconnectedAsync += MqttDisConnectedAsync; // 客户端连接关闭事件
                MqttClientService._mqttClient.ApplicationMessageReceivedAsync += MqttReceivedAsync; // 收到消息事件

                MqttClientService._mqttClient.ConnectAsync(clientOptions);
                iscontinue = true;
            }
            if (!iscontinue)
            {
                Task.Run(() =>
                {
                    Task.Delay(60 * 1000).Wait();
                    MqttConnect();
                });
            }
        }

        /// <summary>
        /// 客户端连接关闭事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task MqttDisConnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            await Task.Delay(20 * 1000);
            _ = MqttClientService._mqttClient.ConnectAsync(clientOptions);
            LogHelper.SysLogWrite("MqttClientHostService", "MqttDisConnectedAsync", "webapi与服务端重新连接", "MQTT");
        }

        /// <summary>
        /// 客户端连接成功事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task MqttConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            var _mainTopic = mainTopic = MqttParam.MqttSubTopicWebApi;
            // 订阅消息主题
            // MqttQualityOfServiceLevel: （QoS）:  0 最多一次，接收者不确认收到消息，并且消息不被发送者存储和重新发送提供与底层 TCP 协议相同的保证。
            // 1: 保证一条消息至少有一次会传递给接收方。发送方存储消息，直到它从接收方收到确认收到消息的数据包。一条消息可以多次发送或传递。
            // 2: 保证每条消息仅由预期的收件人接收一次。级别2是最安全和最慢的服务质量级别，保证由发送方和接收方之间的至少两个请求/响应（四次握手）。
            await MqttClientService._mqttClient.SubscribeAsync(_mainTopic, MqttQualityOfServiceLevel.AtLeastOnce);
            LogHelper.SysLogWrite("MqttClientHostService", "MqttConnectedAsync", "webapi服务端的连接成功并订阅", "MQTT");
        }

        /// <summary>
        /// 收到消息事件
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        private async Task MqttReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            if (!arg.ApplicationMessage.Topic.Contains(mainTopic)) return;

            var buffer = arg.ApplicationMessage.Payload.ToArray();
            if (buffer != null && buffer.Length > 0)
            {
                string strdata = Encoding.UTF8.GetString(buffer);
                if (AppSetting.GetConfig("MqttConfig:LogOpen").ToLower() == "true")
                    LogHelper.SysLogWrite("MqttClientHostService", "MqttReceivedAsync", $"Mqtt接收数据：{strdata}", "MQTT");

                try
                {
                    await Task.Delay(1000);
                    //var mqttdata = strdata.ToObject<MqttDataModel>();
                    //if (mqttdata != null)
                    //{

                    //}
                }
                catch (Exception ex)
                {
                    LogHelper.SysLogWrite("MqttClientHostService", "MqttReceivedAsync", ex.ToString(), "错误");
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(1000);
            if (MqttClientService._mqttClient != null && MqttClientService._mqttClient.IsConnected) MqttClientService._mqttClient.Dispose();
        }
    }
}