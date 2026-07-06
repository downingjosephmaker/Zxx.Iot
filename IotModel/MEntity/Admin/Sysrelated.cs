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
    public class SysRelated : BaseEntity
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
        /// 建筑ID集合
        ///</summary>
        [DisplayName("建筑ID集合")]
        [SugarColumn(ColumnName = "build_ids", IsNullable = true, Length = 1000, ColumnDescription = "建筑ID集合", DefaultValue = "", ColumnDataType = "varchar")]
        public string BuildIds { get; set; }
        /// <summary>
        /// 部门ID集合
        ///</summary>
        [DisplayName("部门ID集合")]
        [SugarColumn(ColumnName = "dept_codes", IsNullable = true, Length = 1000, ColumnDescription = "部门ID集合", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeptCodes { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }
    }
}