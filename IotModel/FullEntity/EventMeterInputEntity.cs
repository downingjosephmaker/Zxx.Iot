using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 抄表录入记录完整类
    ///</summary>
    [DisplayName("抄表录入记录完整类")]
    [FullEntity]
    public class EventMeterInputEntity : EventMeterInput
    {
        /// <summary>
        /// 抄表录入记录拓展类
        ///</summary>
        [DisplayName("抄表录入记录拓展类")]
        public List<Expand_EventHistory> ExpandObjects { get; set; } = new List<Expand_EventHistory>();
    }
}
