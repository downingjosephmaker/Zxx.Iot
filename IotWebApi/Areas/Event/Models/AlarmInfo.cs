using System.ComponentModel;

namespace IotWebApi.Areas.Event.Models
{
    /// <summary>
    /// 告警处理模型
    /// </summary>
    public class HandleAlarm
    {
        /// <summary>
        /// 告警ID
        ///</summary>
        [DisplayName("告警ID")]
        public long SnowId { get; set; }
        /// <summary>
        /// 处理时间
        ///</summary>
        [DisplayName("处理时间")]
        public string CheckTime { get; set; }
        /// <summary>
        /// 处理备注
        ///</summary>
        [DisplayName("处理备注")]
        public string CheckRemark { get; set; }
    }

    /// <summary>
    /// 告警页面曲线返回
    /// </summary>
    public class AlarmChart
    {
        /// <summary>
        /// 报警类型
        ///</summary>
        [DisplayName("报警类型")]
        public long alarmcount { get; set; }
        /// <summary>
        /// 报警类型曲线
        ///</summary>
        [DisplayName("报警类型曲线")]
        public DataChart lxchart { get; set; } = new DataChart();
        /// <summary>
        /// 报警等级曲线
        ///</summary>
        [DisplayName("报警等级曲线")]
        public DataChart djchart { get; set; } = new DataChart();
    }

    /// <summary>
    /// 告警分析
    /// </summary>
    public class RestfulAlarmAnalysisOne
    {
        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        public int BuildId { get; set; }
        /// <summary>
        /// 配电房名称
        ///</summary>
        [DisplayName("配电房名称")]
        public string BuildName { get; set; }
        /// <summary>
        /// 报警处理率
        ///</summary>
        [DisplayName("报警处理率")]
        public string AlarmChuLiLv { get; set; }
        /// <summary>
        /// 报警总数目
        ///</summary>
        [DisplayName("报警总数目")]
        public long AlarmAllCount { get; set; }

    }

    /// <summary>
    /// 告警分析
    /// </summary>
    public class RestfulAlarmAnalysisTwo
    {
        /// <summary>
        /// ID
        ///</summary>
        [DisplayName("ID")]
        public int TypeId { get; set; }
        /// <summary>
        /// 名称
        ///</summary>
        [DisplayName("名称")]
        public string TypeName { get; set; }
        /// <summary>
        /// 报警总数目
        ///</summary>
        [DisplayName("报警总数目")]
        public long AlarmAllCount { get; set; }

    }


}
