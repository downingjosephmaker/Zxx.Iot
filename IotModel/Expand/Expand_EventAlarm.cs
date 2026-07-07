using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 告警日志拓展类
    ///</summary>
    [DisplayName("告警日志拓展类")]
    [Expand]
    public class Expand_EventAlarm
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
        /// 公式名称
        ///</summary>
        [DisplayName("公式名称")]
        public string FormulaName { get; set; }
        /// <summary>
        /// 计算公式
        ///</summary>
        [DisplayName("计算公式")]
        public string JisuanFormula { get; set; }
        /// <summary>
        /// 告警规则ID(§9.2生命周期去重键组成:同设备同规则唯一活动告警;
        /// 离线告警恒为0,历史数据无此字段反序列化默认0)
        ///</summary>
        [DisplayName("告警规则ID")]
        public long RuleId { get; set; }
    }
}