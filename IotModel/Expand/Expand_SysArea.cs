using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 行政区划拓展类
    ///</summary>
    [DisplayName("行政区划拓展类")]
    [Expand]
    public class Expand_SysArea
    {
        /// <summary>
        /// 是否显示
        ///</summary>
        [DisplayName("是否显示")]
        public bool IsDisplay { get; set; } = false;
        /// <summary>
        /// 电力排碳因子
        ///</summary>
        [DisplayName("电力排碳因子")]
        public decimal ElecFactors { get; set; }
        /// <summary>
        /// 水排碳因子
        ///</summary>
        [DisplayName("水排碳因子")]
        public decimal WaterFactors { get; set; } = (decimal)0.35;
        /// <summary>
        /// 纬度
        ///</summary>
        [DisplayName("纬度")]
        public decimal Latitude { get; set; }
        /// <summary>
        /// 经度
        ///</summary>
        [DisplayName("经度")]
        public decimal Longitude { get; set; }
    }
}