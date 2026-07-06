using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 峰谷时段
    /// </summary>
    public enum PeakValley
    {
        [Description("尖峰")]
        尖峰 = 7,

        [Description("高峰")]
        高峰 = 8,

        [Description("平时段")]
        平时段 = 9,

        [Description("低谷")]
        低谷 = 10,

        [Description("深谷")]
        深谷 = 11,
    }
}
