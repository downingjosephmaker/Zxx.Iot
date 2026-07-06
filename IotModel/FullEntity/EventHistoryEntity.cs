using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 历史记录表完整类
    ///</summary>
    [DisplayName("历史记录表完整类")]
    [FullEntity]
    public class EventHistoryEntity : EventHistory
    {
        /// <summary>
        /// 历史记录拓展类
        ///</summary>
        [DisplayName("历史记录拓展类")]
        public List<Expand_EventHistory> ExpandObject { get; set; } = new List<Expand_EventHistory>();
    }
}
