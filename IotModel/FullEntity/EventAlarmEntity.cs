using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 告警日志表完整类
    ///</summary>
    [DisplayName("告警日志表完整类")]
    [FullEntity]
    public class EventAlarmEntity : EventAlarm
    {
        /// <summary>
        /// 告警日志拓展类
        ///</summary>
        [DisplayName("告警日志拓展类")]
        public List<Expand_EventAlarm> ExpandObject { get; set; } = new List<Expand_EventAlarm>();
    }
}
