using System.ComponentModel;

namespace IotWebApi.Areas.Basic.Models
{
    /// <summary>
    /// 前端用Mqtt信息
    /// </summary>
    public class MqttInfo
    {
        /// <summary>
        /// MqttUrl
        ///</summary>
        [DisplayName("MqttUrl")]
        public string MqttUrl { get; set; }
        /// <summary>
        /// MqttUser
        ///</summary>
        [DisplayName("MqttUser")]
        public string MqttUser { get; set; }
        /// <summary>
        /// MqttPass
        ///</summary>
        [DisplayName("MqttPass")]
        public string MqttPass { get; set; }
        /// <summary>
        /// 数据订阅
        ///</summary>
        [DisplayName("数据订阅")]
        public string WebReal { get; set; }
        /// <summary>
        /// 告警订阅
        ///</summary>
        [DisplayName("告警订阅")]
        public string WebAlarm { get; set; }
    }

    /// <summary>
    /// Iot用Mqtt信息
    /// </summary>
    public class MqttInfoIot : MqttInfo
    {
        /// <summary>
        /// MqttIp
        ///</summary>
        [DisplayName("MqttIp")]
        public string MqttIp { get; set; }
        /// <summary>
        /// MqttPort
        ///</summary>
        [DisplayName("MqttPort")]
        public int MqttPort { get; set; }
        /// <summary>
        /// MqttUser
        ///</summary>
        [DisplayName("MqttUser")]
        public string MqttUser { get; set; }
        /// <summary>
        /// MqttPass
        ///</summary>
        [DisplayName("MqttPass")]
        public string MqttPass { get; set; }
        /// <summary>
        /// 配置订阅
        ///</summary>
        [DisplayName("配置订阅")]
        public List<string> TypeKeys { get; set; } = new List<string>();
        /// <summary>
        /// 配置后上报订阅
        ///</summary>
        [DisplayName("配置后上报订阅")]
        public string ZzReturn { get; set; }
    }

}
