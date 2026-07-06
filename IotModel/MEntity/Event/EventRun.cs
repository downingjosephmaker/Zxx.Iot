using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 运行日志
    ///</summary>
    [DisplayName("运行日志")]
    [SplitTable(SplitType.Month, typeof(SnowSplitService))]
    [SugarTable(TableName = "event_run", TableDescription = "运行日志", IsDisabledUpdateAll = true)]
    public class EventRun : EventBase
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
        /// 事件类型(设备离线|设备通信恢复)
        ///</summary>
        [DisplayName("事件类型(设备离线|设备通信恢复)")]
        [SugarColumn(ColumnName = "event_type", IsNullable = true, Length = 50, ColumnDescription = "事件类型(设备离线|设备通信恢复)", DefaultValue = "设备离线", ColumnDataType = "varchar")]
        public string EventType { get; set; }
        /// <summary>
        /// 记录详情
        ///</summary>
        [DisplayName("记录详情")]
        [SugarColumn(ColumnName = "event_content", IsNullable = true, Length = 200, ColumnDescription = "记录详情", DefaultValue = "", ColumnDataType = "varchar")]
        public string EventContent { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_EventRun))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}