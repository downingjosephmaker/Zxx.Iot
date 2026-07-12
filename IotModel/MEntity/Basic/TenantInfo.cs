using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 租户信息
    ///</summary>
    [DisplayName("租户信息")]
    [EntityCache]
    [SugarTable(TableName = "tenant_info", TableDescription = "租户信息", IsDisabledUpdateAll = true)]
    public class TenantInfo : BaseEntity
    {
        /// <summary>
        /// 租户ID
        ///</summary>
        [DisplayName("租户ID")]
        [SugarColumn(ColumnName = "tenant_id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
        /// <summary>
        /// 上级租户ID(0=根租户)
        ///</summary>
        [DisplayName("上级租户ID")]
        [SugarColumn(ColumnName = "parent_id", ColumnDescription = "上级租户ID(0=根)", DefaultValue = "0", ColumnDataType = "int")]
        public int ParentId { get; set; }
        /// <summary>
        /// 租户层级(1=顶级)
        ///</summary>
        [DisplayName("租户层级")]
        [SugarColumn(ColumnName = "tree_level", ColumnDescription = "租户层级(1=顶级)", DefaultValue = "1", ColumnDataType = "int")]
        public int TreeLevel { get; set; }
        /// <summary>
        /// 祖先链(形如 |1|3|7|)
        ///</summary>
        [DisplayName("祖先链")]
        [SugarColumn(ColumnName = "full_code", IsNullable = true, Length = 200, ColumnDescription = "祖先链(形如 |1|3|7|)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullCode { get; set; }
        /// <summary>
        /// 租户全称(全路径)
        ///</summary>
        [DisplayName("租户全称")]
        [SugarColumn(ColumnName = "full_name", IsNullable = true, Length = 400, ColumnDescription = "租户全称(全路径)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullName { get; set; }
        /// <summary>
        /// 是否有子租户
        ///</summary>
        [DisplayName("是否有子租户")]
        [SugarColumn(ColumnName = "has_child", Length = 1, ColumnDescription = "是否有子租户", DefaultValue = "0", ColumnDataType = "bit")]
        public bool HasChild { get; set; }
        /// <summary>
        /// 租户名称
        ///</summary>
        [DisplayName("租户名称")]
        [SugarColumn(ColumnName = "tenant_name", IsNullable = true, Length = 50, ColumnDescription = "租户名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string TenantName { get; set; }
        /// <summary>
        /// 备注
        ///</summary>
        [DisplayName("备注")]
        [SugarColumn(ColumnName = "remark", IsNullable = true, Length = 300, ColumnDescription = "备注", DefaultValue = "", ColumnDataType = "varchar")]
        public string Remark { get; set; }
    }
}
