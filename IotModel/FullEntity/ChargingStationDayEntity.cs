using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 充电桩统计日表完整类
    ///</summary>
    [DisplayName("充电桩统计日表完整类")]
    [FullEntity]
    public class ChargingStationDayEntity : ChargingStationDay
    {
        /// <summary>
        /// 充电桩统计日表拓展属性
        ///</summary>
        [DisplayName("充电桩统计日表拓展属性")]
        public List<Expand_ChargingStationDay> ExpandObjects { get; set; } = new List<Expand_ChargingStationDay>();
    }
}
