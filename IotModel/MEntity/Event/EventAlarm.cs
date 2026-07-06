using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 告警日志
    ///</summary>
    [DisplayName("告警日志")]
    [SplitTable(SplitType.Month, typeof(SnowSplitService))]
    [SugarTable(TableName = "event_alarm", TableDescription = "告警日志", IsDisabledUpdateAll = true)]
    public class EventAlarm : EventBase
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [SplitField] //分表字段
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 事件类型
        ///</summary>
        [DisplayName("事件类型")]
        [SugarColumn(ColumnName = "event_type", IsNullable = true, Length = 50, ColumnDescription = "事件类型", DefaultValue = "", ColumnDataType = "varchar")]
        public string EventType { get; set; }
        /// <summary>
        /// 报警等级
        ///</summary>
        [DisplayName("报警等级")]
        [SugarColumn(ColumnName = "alarm_grade", IsNullable = true, Length = 20, ColumnDescription = "报警等级", DefaultValue = "", ColumnDataType = "varchar")]
        public string AlarmGrade { get; set; }
        /// <summary>
        /// 报警类型
        ///</summary>
        [DisplayName("报警类型")]
        [SugarColumn(ColumnName = "alarm_type", IsNullable = true, Length = 20, ColumnDescription = "报警类型", DefaultValue = "", ColumnDataType = "varchar")]
        public string AlarmType { get; set; }
        /// <summary>
        /// 报警内容
        ///</summary>
        [DisplayName("报警内容")]
        [SugarColumn(ColumnName = "alarm_value", IsNullable = true, Length = 300, ColumnDescription = "报警内容", DefaultValue = "", ColumnDataType = "varchar")]
        public string AlarmValue { get; set; }
        /// <summary>
        /// 处理结果(已处理,未处理)
        ///</summary>
        [DisplayName("处理结果(已处理,未处理)")]
        [SugarColumn(ColumnName = "check_result", IsNullable = false, Length = 10, ColumnDescription = "处理结果(已处理,未处理)", DefaultValue = "未处理", ColumnDataType = "varchar")]
        public string CheckResult { get; set; }
        /// <summary>
        /// 处理用户ID
        ///</summary>
        [DisplayName("处理用户ID")]
        [SugarColumn(ColumnName = "check_user_id", ColumnDescription = "处理用户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int CheckUserId { get; set; }
        /// <summary>
        /// 处理用户名称
        ///</summary>
        [DisplayName("处理用户名称")]
        [SugarColumn(ColumnName = "check_user_name", IsNullable = true, Length = 50, ColumnDescription = "处理用户名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string CheckUserName { get; set; }
        /// <summary>
        /// 处理时间
        ///</summary>
        [DisplayName("处理时间")]
        [SugarColumn(ColumnName = "check_time", IsNullable = true, Length = 20, ColumnDescription = "处理时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string CheckTime { get; set; }
        /// <summary>
        /// 处理备注
        ///</summary>
        [DisplayName("处理备注")]
        [SugarColumn(ColumnName = "check_remark", IsNullable = true, Length = 200, ColumnDescription = "处理备注", DefaultValue = "", ColumnDataType = "varchar")]
        public string CheckRemark { get; set; }
        /// <summary>
        /// 恢复情况(已恢复,未恢复)
        ///</summary>
        [DisplayName("恢复情况(已恢复,未恢复)")]
        [SugarColumn(ColumnName = "is_restore", IsNullable = false, Length = 10, ColumnDescription = "恢复情况(已恢复,未恢复)", DefaultValue = "未恢复", ColumnDataType = "varchar")]
        public string IsRestore { get; set; }
        /// <summary>
        /// 恢复时间
        ///</summary>
        [DisplayName("恢复时间")]
        [SugarColumn(ColumnName = "restore_time", IsNullable = true, Length = 20, ColumnDescription = "恢复时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string RestoreTime { get; set; }
        /// <summary>
        /// 告警时长(分)
        ///</summary>
        [DisplayName("告警时长(分)")]
        [SugarColumn(ColumnName = "alarm_time_range", ColumnDescription = "告警时长(分)", DefaultValue = "0", ColumnDataType = "int")]
        public int AlarmTimeRange { get; set; }
        /// <summary>
        /// 人工操作次数
        ///</summary>
        [DisplayName("人工操作次数")]
        [SugarColumn(ColumnName = "alarm_opt_count", ColumnDescription = "人工操作次数", DefaultValue = "0", ColumnDataType = "int")]
        public int AlarmOptCount { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_EventAlarm))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}