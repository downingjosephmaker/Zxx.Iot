using CenboEventBus;
using IotLog;
using MQTTnet;
using MQTTnet.Protocol;
using System.Text;

namespace IotWebApi.Services.Mqtt
{
    public class MqttClientService
    {
        public static IMqttClient _mqttClient { get; set; }

        /// <summary>
        /// 插件事件总线静态桥(应用启动后由Program赋值;Quartz任务无构造注入,
        /// MQTT上行消息经此并入插件上行管道进DataPointIngestService)
        /// </summary>
        public static IEventBus<PluginEvent> EventBus { get; set; }

        /// <summary>
        /// MQTT客户端上行的虚拟插件标识(与插件GUID同构,用于上行事件溯源)
        /// </summary>
        public const string MqttPluginGuid = "0af3b6c9d2e5081b4a7c0d3e6f9a2b5c";
        public const string TcpUplinkPluginGuid = "1b0c4d7e2f6a093c5b8d1e4f7a0b3c6d";   // TCP 南向上行溯源
        public const string UdpUplinkPluginGuid = "2c1d5e8f3a7b0a4d6c9e2f5a8b1c4d7e";   // UDP 南向上行溯源

        /// <summary>
        /// 协议脚本服务静态桥(应用启动后由Program赋值;
        /// 非JSON载荷按产品挂JS脚本解码,§6.5)
        /// </summary>
        public static ProtocolScriptService ScriptService { get; set; }

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
