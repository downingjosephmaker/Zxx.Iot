using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 极值记录日表完整类
    ///</summary>
    [DisplayName("极值记录日表完整类")]
    [FullEntity]
    public class EventPeakDayEntity : EventPeakDay
    {
        /// <summary>
        /// 极值记录日表拓展类
        ///</summary>
        [DisplayName("极值记录日表拓展类")]
        public List<Expand_EventPeakDay> ExpandObjects { get; set; } = new List<Expand_EventPeakDay>();
    }
}
