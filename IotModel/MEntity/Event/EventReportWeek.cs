using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 统计记录周表
    ///</summary>
    [DisplayName("统计记录周表")]
    [SplitTable(SplitType.Year, typeof(SnowSplitService))]
    [SugarTable(TableName = "event_report_week", TableDescription = "统计记录周表", IsDisabledUpdateAll = true)]
    public class EventReportWeek : EventBase
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
        /// 年周数
        ///</summary>
        [DisplayName("年周数")]
        [SugarColumn(ColumnName = "week_num", Length = 16, ColumnDescription = "年周数", DefaultValue = "", ColumnDataType = "varchar")]
        public string WeekNum { get; set; }

        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_EventReportWeek))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}