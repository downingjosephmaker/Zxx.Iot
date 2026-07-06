using IotLog;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text;

namespace IotWebApi.Services.Mqtt
{
    public class MqttClientService
    {
        public static IMqttClient _mqttClient { get; set; }

        public static bool MqttPublish(string publishtopic, string data)
        {
            bool isresult = false;
            try
            {
                if (_mqttClient != null && _mqttClient.IsConnected)
                {
                    var message = new MqttApplicationMessage
                    {
                        Topic = publishtopic,
                        PayloadSegment = new System.ArraySegment<byte>(Encoding.UTF8.GetBytes(data)),
                        QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                        Retain = false  // 服务端是否保留消息。true为保留，如果有新的订阅者连接，就会立马收到该消息。
                    };
                    var ret = _mqttClient.PublishAsync(message).Result;
                    if (ret.IsSuccess)
                    {
                        isresult = true;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("MqttClientService", "MqttPublish", ex.ToString(), "错误");
            }
            return isresult;
        }

    }

}
