using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 充电桩统计月表完整类
    ///</summary>
    [DisplayName("充电桩统计月表完整类")]
    [FullEntity]
    public class ChargingStationMonthEntity : ChargingStationMonth
    {
        /// <summary>
        /// 充电桩统计月表拓展属性
        ///</summary>
        [DisplayName("充电桩统计月表拓展属性")]
        public List<Expand_ChargingStationDay> ExpandObjects { get; set; } = new List<Expand_ChargingStationDay>();
    }
}
