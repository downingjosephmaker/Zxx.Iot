using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// JS协议脚本版本历史(§6.4:每次保存快照一条,支撑diff对比与一键回滚;
    /// 追加型历史表不挂EntityCache)
    ///</summary>
    [DisplayName("JS协议脚本版本历史")]
    [SugarTable(TableName = "protocol_script_history", TableDescription = "JS协议脚本版本历史", IsDisabledUpdateAll = true)]
    public class ProtocolScriptHistory : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 所属脚本主键(protocol_script.snow_id)
        ///</summary>
        [DisplayName("所属脚本主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "script_id", Length = 20, ColumnDescription = "所属脚本主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long ScriptId { get; set; }
        /// <summary>
        /// 版本号(保存时的快照版本)
        ///</summary>
        [DisplayName("版本号")]
        [SugarColumn(ColumnName = "version", ColumnDescription = "版本号", DefaultValue = "1", ColumnDataType = "int")]
        public int Version { get; set; } = 1;
        /// <summary>
        /// 脚本内容快照
        ///</summary>
        [DisplayName("脚本内容快照")]
        [SugarColumn(ColumnName = "script_content", IsNullable = true, ColumnDescription = "脚本内容快照", ColumnDataType = "text")]
        public string ScriptContent { get; set; }
    }
}
