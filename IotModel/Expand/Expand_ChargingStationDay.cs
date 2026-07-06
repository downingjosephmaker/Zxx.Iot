using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 充电桩统计日表拓展属性
    ///</summary>
    [DisplayName("充电桩统计日表拓展属性")]
    [Expand]
    public class Expand_ChargingStationDay
    {
        /// <summary>
        /// 小时数
        ///</summary>
        [DisplayName("小时数")]
        public int Hour { get; set; } = 0;
        /// <summary>
        /// 充电次数
        ///</summary>
        [DisplayName("充电次数")]
        public int ChargingCount { get; set; } = 0;
        /// <summary>
        /// 充电时长(分)
        ///</summary>
        [DisplayName("充电时长(分)")]
        public int ChargingDuration { get; set; } = 0;
        /// <summary>
        /// 充电能耗
        /// </summary>
        [DisplayName("充电能耗")]
        public decimal ChargingEnergy { get; set; } = 0;
    }
}
