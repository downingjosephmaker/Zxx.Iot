using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 短信记录
    ///</summary>
    [DisplayName("短信记录")]
    [SplitTable(SplitType.Month, typeof(SnowSplitService))]
    [SugarTable(TableName = "event_note_record", TableDescription = "短信记录", IsDisabledUpdateAll = true)]
    public class EventNoteRecord
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
        /// 记录时间
        ///</summary>
        [DisplayName("记录时间")]
        [SugarColumn(ColumnName = "event_time", IsNullable = true, Length = 20, ColumnDescription = "记录时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string EventTime { get; set; }
        /// <summary>
        /// 业务类型(未签退通知)
        ///</summary>
        [DisplayName("业务类型(未签退通知)")]
        [SugarColumn(ColumnName = "note_type", Length = 20, ColumnDescription = "业务类型(未签退通知)", DefaultValue = "未签退通知", ColumnDataType = "varchar")]
        public string NoteType { get; set; }
        /// <summary>
        /// 接收人姓名
        ///</summary>
        [DisplayName("接收人姓名")]
        [SugarColumn(ColumnName = "recipient_name", IsNullable = true, Length = 50, ColumnDescription = "接收人姓名", DefaultValue = "", ColumnDataType = "varchar")]
        public string RecipientName { get; set; }
        /// <summary>
        /// 接收人所在部门
        ///</summary>
        [DisplayName("接收人所在部门")]
        [SugarColumn(ColumnName = "recipient_depart_name", IsNullable = true, Length = 200, ColumnDescription = "接收人所在部门", DefaultValue = "", ColumnDataType = "varchar")]
        public string RecipientDepartName { get; set; }
        /// <summary>
        /// 接收人号码
        ///</summary>
        [DisplayName("接收人号码")]
        [SugarColumn(ColumnName = "recipient_mobile_phone", IsNullable = true, Length = 13, ColumnDescription = "接收人号码", DefaultValue = "", ColumnDataType = "varchar")]
        public string RecipientMobilePhone { get; set; }
        /// <summary>
        /// 下发内容
        ///</summary>
        [DisplayName("下发内容")]
        [SugarColumn(ColumnName = "note_content", IsNullable = true, Length = 500, ColumnDescription = "下发内容", DefaultValue = "", ColumnDataType = "varchar")]
        public string NoteContent { get; set; }
        /// <summary>
        /// 下发结果
        ///</summary>
        [DisplayName("下发结果")]
        [SugarColumn(ColumnName = "send_result", IsNullable = true, Length = 20, ColumnDescription = "下发结果", DefaultValue = "", ColumnDataType = "varchar")]
        public string SendResult { get; set; }
    }
}