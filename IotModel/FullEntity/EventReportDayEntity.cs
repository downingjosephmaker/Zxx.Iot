using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 统计记录日表完整类
    ///</summary>
    [DisplayName("统计记录日表完整类")]
    [FullEntity]
    public class EventReportDayEntity : EventReportDay
    {
        /// <summary>
        /// 统计记录日表拓展类
        ///</summary>
        [DisplayName("统计记录日表拓展类")]
        public List<Expand_EventReportDay> ExpandObjects { get; set; } = new List<Expand_EventReportDay>();
    }
}
