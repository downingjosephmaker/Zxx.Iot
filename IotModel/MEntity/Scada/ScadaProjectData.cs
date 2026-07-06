using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 组态项目内容表
    ///</summary>
    [DisplayName("组态项目内容表")]
    [EntityCache]
    [SugarTable(TableName = "scada_project_data", TableDescription = "组态项目内容表", IsDisabledUpdateAll = true)]
    public class ScadaProjectData : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }

        /// <summary>
        /// 项目ID
        ///</summary>
        [DisplayName("项目ID")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "project_id", Length = 20, ColumnDescription = "项目ID", DefaultValue = "0", ColumnDataType = "bigint")]
        public long ProjectId { get; set; }

        /// <summary>
        /// 项目内容
        ///</summary>
        [DisplayName("项目内容")]
        [SugarColumn(ColumnName = "content_data", IsNullable = true, ColumnDescription = "项目内容", ColumnDataType = "mediumtext")]
        public string ContentData { get; set; }
    }
}