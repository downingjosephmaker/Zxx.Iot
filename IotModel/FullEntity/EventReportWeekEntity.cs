using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 统计记录周表完整类
    ///</summary>
    [DisplayName("统计记录周表完整类")]
    [FullEntity]
    public class EventReportWeekEntity : EventReportWeek
    {
        /// <summary>
        /// 统计记录周表拓展类
        ///</summary>
        [DisplayName("统计记录周表拓展类")]
        public List<Expand_EventReportWeek> ExpandObjects { get; set; } = new List<Expand_EventReportWeek>();
    }
}
