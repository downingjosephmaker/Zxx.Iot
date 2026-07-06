using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 单位表拓展类
    ///</summary>
    [DisplayName("单位表拓展类")]
    [Expand]
    public class Expand_BasicunitInfo
    {
        /// <summary>
        /// 节能率%(*100)
        ///</summary>
        [DisplayName("节能率%")]
        public decimal EnergyRate { get; set; }
        /// <summary>
        /// 大屏跳转路径
        ///</summary>
        [DisplayName("大屏跳转路径")]
        public string RouterPath { get; set; }
    }
}
