using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// logo配置
    ///</summary>
    [DisplayName("logo配置")]
    [SugarTable(TableName = "admin_logoparam", TableDescription = "logo配置", IsDisabledUpdateAll = true)]
    public class AdminLogoparam : ITenantEntity
    {
        /// <summary>
        /// 配置ID
        ///</summary>
        [DisplayName("配置ID")]
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "配置ID", DefaultValue = "0", ColumnDataType = "int")]
        public int Id { get; set; }
        /// <summary>
        /// 系统标题
        ///</summary>
        [DisplayName("系统标题")]
        [SugarColumn(ColumnName = "system_title", IsNullable = true, Length = 30, ColumnDescription = "系统标题", DefaultValue = "", ColumnDataType = "varchar")]
        public string SystemTitle { get; set; }
        /// <summary>
        /// 系统Logo路径
        ///</summary>
        [DisplayName("系统Logo路径")]
        [SugarColumn(ColumnName = "system_logo", IsNullable = true, ColumnDescription = "系统Logo路径", DefaultValue = "", ColumnDataType = "varchar")]
        public string SystemLogo { get; set; }
        /// <summary>
        /// 租户ID
        ///</summary>
        [DisplayName("租户ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
    }
}