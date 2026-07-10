using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 大屏项目表
    ///</summary>
    [DisplayName("大屏项目表")]
    [EntityCache]
    [SugarTable(TableName = "dash_project", TableDescription = "大屏项目表", IsDisabledUpdateAll = true)]
    public class DashProject : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }

        /// <summary>
        /// 项目名称
        ///</summary>
        [DisplayName("项目名称")]
        [SugarColumn(ColumnName = "project_name", Length = 100, ColumnDescription = "项目名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string ProjectName { get; set; }

        /// <summary>
        /// 项目描述
        ///</summary>
        [DisplayName("项目描述")]
        [SugarColumn(ColumnName = "project_desc", IsNullable = true, Length = 500, ColumnDescription = "项目描述", DefaultValue = "", ColumnDataType = "varchar")]
        public string ProjectDesc { get; set; }

        /// <summary>
        /// 发布状态(0:未发布 1:发布)
        ///</summary>
        [DisplayName("发布状态(0:未发布 1:发布)")]
        [IntRange(0, 1, ErrorMessage = "项目状态值只能为0或1")]
        [SugarColumn(ColumnName = "project_status", ColumnDescription = "发布状态(0:未发布 1:发布)", DefaultValue = "0", ColumnDataType = "tinyint")]
        public int ProjectStatus { get; set; }

        /// <summary>
        /// 缩略图路径
        ///</summary>
        [DisplayName("缩略图路径")]
        [SugarColumn(ColumnName = "thumbnail", IsNullable = true, Length = 200, ColumnDescription = "缩略图路径", DefaultValue = "", ColumnDataType = "varchar")]
        public string Thumbnail { get; set; }

        /// <summary>
        /// 默认状态(0:未设置 1:默认)
        ///</summary>
        [DisplayName("默认状态(0:未设置 1:默认)")]
        [IntRange(0, 1, ErrorMessage = "默认状态值只能为0或1")]
        [SugarColumn(ColumnName = "project_default", ColumnDescription = "默认状态(0:未设置 1:默认)", DefaultValue = "0", ColumnDataType = "tinyint")]
        public int ProjectDefault { get; set; }

        /// <summary>
        /// 运行态访问地址
        ///</summary>
        [DisplayName("运行态访问地址")]
        [SugarColumn(ColumnName = "runtime_url", IsNullable = true, Length = 300, ColumnDescription = "运行态访问地址", DefaultValue = "", ColumnDataType = "varchar")]
        public string RuntimeUrl { get; set; }

        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }

        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}