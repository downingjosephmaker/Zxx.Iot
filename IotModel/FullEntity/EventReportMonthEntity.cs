using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 统计记录月表完整类
    ///</summary>
    [DisplayName("统计记录月表完整类")]
    [FullEntity]
    public class EventReportMonthEntity : EventReportMonth
    {
        /// <summary>
        /// 统计记录月表拓展类
        ///</summary>
        [DisplayName("统计记录月表拓展类")]
        public List<Expand_EventReportMonth> ExpandObjects { get; set; } = new List<Expand_EventReportMonth>();
    }
}
