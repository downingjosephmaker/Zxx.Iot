using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 状态变化日志
    ///</summary>
    [DisplayName("状态变化日志")]
    [SplitTable(SplitType.Month, typeof(SnowSplitService))]
    [SugarTable(TableName = "event_signal", TableDescription = "状态变化日志", IsDisabledUpdateAll = true)]
    public class EventSignal : EventBase
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
        /// 事件类型(状态变化 意外情况)
        ///</summary>
        [DisplayName("事件类型(状态变化 意外情况)")]
        [SugarColumn(ColumnName = "event_type", IsNullable = false, Length = 50, ColumnDescription = "事件类型(状态变化 意外情况)", DefaultValue = "状态变化", ColumnDataType = "varchar")]
        public string EventType { get; set; }
        /// <summary>
        /// 内容
        ///</summary>
        [DisplayName("内容")]
        [SugarColumn(ColumnName = "event_value", IsNullable = true, Length = 200, ColumnDescription = "内容", DefaultValue = "", ColumnDataType = "varchar")]
        public string EventValue { get; set; }
        /// <summary>
        /// 详情
        ///</summary>
        [DisplayName("详情")]
        [SugarColumn(ColumnName = "event_content", IsNullable = true, Length = 200, ColumnDescription = "详情", DefaultValue = "", ColumnDataType = "varchar")]
        public string EventContent { get; set; }
    }
}
