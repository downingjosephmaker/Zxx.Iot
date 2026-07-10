using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 用户建筑权限
    ///</summary>
    [DisplayName("用户建筑权限")]
    [EntityCache]
    [SugarTable(TableName = "sys_related", TableDescription = "用户建筑权限", IsDisabledUpdateAll = true)]
    public class SysRelated : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 用户ID
        ///</summary>
        [DisplayName("用户ID")]
        [SugarColumn(ColumnName = "user_id", ColumnDescription = "用户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UserId { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
    }
}
