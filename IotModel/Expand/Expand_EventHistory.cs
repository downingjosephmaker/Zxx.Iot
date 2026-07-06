using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 历史记录拓展属性
    ///</summary>
    [DisplayName("历史记录拓展属性")]
    [Expand]
    public class Expand_EventHistory
    {
        /// <summary>
        /// 参数编码
        ///</summary>
        [DisplayName("参数编码")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称
        ///</summary>
        [DisplayName("参数名称")]
        public string ParamName { get; set; }
        /// <summary>
        /// 参数值
        ///</summary>
        [DisplayName("参数值")]
        public string ParamValue { get; set; }
        /// <summary>
        /// 值单位
        ///</summary>
        [DisplayName("值单位")]
        public string ValueUnit { get; set; }
        /// <summary>
        /// 是否告警(0:正常 1:告警)
        ///</summary>
        [DisplayName("是否告警(0:正常 1:告警)")]
        public int IsAlarm { get; set; }
        /// <summary>
        /// 告警SnowId
        ///</summary>
        [DisplayName("告警SnowId")]
        [JsonConverter(typeof(ValueToStringConverter))]
        public long AlarmSnowId { get; set; }
    }
}
