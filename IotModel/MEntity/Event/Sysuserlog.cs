using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 用户日志
    ///</summary>
    [DisplayName("用户日志")]
    [SplitTable(SplitType.Month, typeof(SnowSplitService))]
    [SugarTable(TableName = "sysuser_log", TableDescription = "用户日志", IsDisabledUpdateAll = true)]
    public class SysuserLog
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
        /// 日志类型(登录,退出)
        ///</summary>
        [DisplayName("日志类型(登录,退出)")]
        [SugarColumn(ColumnName = "log_type", IsNullable = true, Length = 20, ColumnDescription = "日志类型(登录,退出)", DefaultValue = "", ColumnDataType = "varchar")]
        public string LogType { get; set; }
        /// <summary>
        /// 操作来源(Web、Android、APP)
        ///</summary>
        [DisplayName("操作来源(Web、Android、APP)")]
        [SugarColumn(ColumnName = "source_type", IsNullable = true, Length = 10, ColumnDescription = "操作来源(Web、Android、APP)", DefaultValue = "", ColumnDataType = "varchar")]
        public string SourceType { get; set; }
        /// <summary>
        /// 操作人ID
        ///</summary>
        [DisplayName("操作人ID")]
        [SugarColumn(ColumnName = "create_user_id", ColumnDescription = "操作人ID", DefaultValue = "0", ColumnDataType = "int")]
        public int CreateUserId { get; set; }
        /// <summary>
        /// 操作人名称
        ///</summary>
        [DisplayName("操作人名称")]
        [SugarColumn(ColumnName = "create_user_name", IsNullable = true, Length = 50, ColumnDescription = "操作人名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string CreateUserName { get; set; }
        /// <summary>
        /// 操作时间
        ///</summary>
        [DisplayName("操作时间")]
        [SugarColumn(ColumnName = "create_time", IsNullable = true, Length = 20, ColumnDescription = "操作时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string CreateTime { get; set; }
        /// <summary>
        /// 操作人IP
        ///</summary>
        [DisplayName("操作人IP")]
        [SugarColumn(ColumnName = "create_ip", IsNullable = true, Length = 30, ColumnDescription = "操作人IP", DefaultValue = "", ColumnDataType = "varchar")]
        public string CreateIp { get; set; }
    }
}