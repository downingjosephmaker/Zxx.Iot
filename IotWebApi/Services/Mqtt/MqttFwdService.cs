using CenBoCommon.Zxx;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System.Text;

namespace IotWebApi.Services.Mqtt
{
    public class MqttFwdService
    {
        public static MqttServer _mqttServer { get; set; }

        public static void PublishData(string publishtopic, string data)
        {
            if (_mqttServer.IsStarted)
            {
                var message = new MqttApplicationMessage
                {
                    Topic = publishtopic,
                    PayloadSegment = new System.ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                    Retain = false  // 服务端是否保留消息。true为保留，如果有新的订阅者连接，就会立马收到该消息。
                };

                _mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(message) // 发送消息给有订阅 topic_01的客户端
                {
                    SenderClientId = $"IotWebApiApi_Server_{SnowModel.Instance.NewId()}"
                }).GetAwaiter().GetResult();
            }
        }

    }

}
