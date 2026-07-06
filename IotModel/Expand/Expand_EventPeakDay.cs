using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 极值记录日表拓展类
    ///</summary>
    [DisplayName("极值记录日表拓展类")]
    [Expand]
    public class Expand_EventPeakDay
    {
        /// <summary>
        /// 参数编码
        ///</summary>
        [DisplayName("参数编码")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 参数名称
        ///</summary>
        [DisplayName("参数名称")]
        public string ParamName { get; set; }
        /// <summary>
        /// 最大值
        ///</summary>
        [DisplayName("最大值")]
        public string MaxValue { get; set; }
        /// <summary>
        /// 最大值时间
        ///</summary>
        [DisplayName("最大值时间")]
        public string MaxTime { get; set; }
        /// <summary>
        /// 最小值
        ///</summary>
        [DisplayName("最小值")]
        public string MinValue { get; set; }
        /// <summary>
        /// 最小值时间
        ///</summary>
        [DisplayName("最小值时间")]
        public string MinTime { get; set; }
        /// <summary>
        /// 平均值
        ///</summary>
        [DisplayName("平均值")]
        public string AvgValue { get; set; }
        /// <summary>
        /// 累加值
        ///</summary>
        [DisplayName("累加值")]
        public string SumValue { get; set; }
        /// <summary>
        /// 累加值时间
        ///</summary>
        [DisplayName("累加值时间")]
        public string SumTime { get; set; }
        /// <summary>
        /// 启始值
        ///</summary>
        [DisplayName("启始值")]
        public string FirstValue { get; set; }
        /// <summary>
        /// 启始值时间
        ///</summary>
        [DisplayName("启始值时间")]
        public string FirstTime { get; set; }
        /// <summary>
        /// 终止值
        ///</summary>
        [DisplayName("终止值")]
        public string LastValue { get; set; }
        /// <summary>
        /// 终止值时间
        ///</summary>
        [DisplayName("终止值时间")]
        public string LastTime { get; set; }
        /// <summary>
        /// 值单位
        ///</summary>
        [DisplayName("值单位")]
        public string ValueUnit { get; set; }
        /// <summary>
        /// 计算次数
        ///</summary>
        [DisplayName("计算次数")]
        public int JisuanCount { get; set; }
    }
}
