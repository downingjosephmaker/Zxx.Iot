using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 统计日表拓展类
    ///</summary>
    [DisplayName("统计日表拓展类")]
    [Expand]
    public class Expand_EventReportDay : Expand_EventReportWeek
    {
        /// <summary>
        /// 启始值
        ///</summary>
        [DisplayName("启始值")]
        public string FirstValue { get; set; }
        /// <summary>
        /// 上个小时数值
        ///</summary>
        [DisplayName("上个小时数值")]
        public string LastHourValue { get; set; }
        /// <summary>
        /// 上个小时数
        ///</summary>
        [DisplayName("上个小时数")]
        public int LastHourNum { get; set; }
        /// <summary>
        /// 当前值
        ///</summary>
        [DisplayName("当前值")]
        public string CurrentValue { get; set; }
    }
}
