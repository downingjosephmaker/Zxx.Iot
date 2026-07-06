using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 控制日志
    ///</summary>
    [DisplayName("控制日志")]
    [SplitTable(SplitType.Month, typeof(SnowSplitService))]
    [SugarTable(TableName = "event_control", TableDescription = "控制日志", IsDisabledUpdateAll = true)]
    public class EventControl : EventBase
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
        /// 操作结果(成功 失败)
        ///</summary>
        [DisplayName("操作结果(成功 失败)")]
        [SugarColumn(ColumnName = "opt_result", Length = 10, ColumnDescription = "操作结果(成功 失败)", DefaultValue = "成功", ColumnDataType = "varchar")]
        public string OptResult { get; set; }
        /// <summary>
        /// 操作内容描述(包含开始和结束时间点)
        ///</summary>
        [DisplayName("操作内容描述(包含开始和结束时间点)")]
        [SugarColumn(ColumnName = "opt_content", IsNullable = true, Length = 300, ColumnDescription = "操作内容描述(包含开始和结束时间点)", DefaultValue = "", ColumnDataType = "varchar")]
        public string OptContent { get; set; }
        /// <summary>
        /// 控制Josn
        ///</summary>
        [DisplayName("控制Josn")]
        [SugarColumn(ColumnName = "opt_josn", IsNullable = true, ColumnDescription = "控制Josn", ColumnDataType = "text")]
        public string OptJosn { get; set; }
        /// <summary>
        /// 是否批量(0:否 1:是)
        ///</summary>
        [DisplayName("是否批量(0:否 1:是)")]
        [SugarColumn(ColumnName = "opt_batch", ColumnDescription = "是否批量(0:否 1:是)", DefaultValue = "0", ColumnDataType = "int")]
        public int OptBatch { get; set; }
        /// <summary>
        /// 雪花标识
        ///</summary>
        [DisplayName("雪花标识")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "opt_batch_id", IsNullable = true, Length = 20, ColumnDescription = "雪花标识", DefaultValue = "0", ColumnDataType = "bigint")]
        public long OptBatchId { get; set; }
        /// <summary>
        /// 操作用户ID
        ///</summary>
        [DisplayName("操作用户ID")]
        [SugarColumn(ColumnName = "event_user_id", ColumnDescription = "操作用户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int EventUserId { get; set; }
        /// <summary>
        /// 操作用户名称
        ///</summary>
        [DisplayName("操作用户名称")]
        [SugarColumn(ColumnName = "event_user_name", IsNullable = true, Length = 50, ColumnDescription = "操作用户名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string EventUserName { get; set; }
        /// <summary>
        /// 控制来源(Web、Android、APP)
        ///</summary>
        [DisplayName("控制来源(Web、Android、APP)")]
        [SugarColumn(ColumnName = "source_type", IsNullable = true, Length = 10, ColumnDescription = "控制来源(Web、Android、APP)", DefaultValue = "", ColumnDataType = "varchar")]
        public string SourceType { get; set; }
        /// <summary>
        /// 链路来源(Web、API、Service)
        ///</summary>
        [DisplayName("链路来源(Web、API、Service)")]
        [SugarColumn(ColumnName = "link_type", IsNullable = true, Length = 10, ColumnDescription = "链路来源(Web、API、Service)", DefaultValue = "", ColumnDataType = "varchar")]
        public string LinkType { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_EventControl))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}