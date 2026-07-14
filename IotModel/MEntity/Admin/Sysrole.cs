using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 角色管理
    ///</summary>
    [DisplayName("角色管理")]
    [EntityCache]
    [SugarTable(TableName = "sys_role", TableDescription = "角色管理", IsDisabledUpdateAll = true)]
    public class SysRole : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// 租户ID(0=平台共享角色,超管维护;非0=某租户自建角色)
        ///</summary>
        [DisplayName("租户ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
        /// <summary>
        /// 角色ID
        ///</summary>
        [DisplayName("角色ID")]
        [SugarColumn(ColumnName = "role_id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "角色ID", DefaultValue = "0", ColumnDataType = "int")]
        public int RoleId { get; set; }
        /// <summary>
        /// 角色名称
        ///</summary>
        [DisplayName("角色名称")]
        [SugarColumn(ColumnName = "role_name", Length = 30, ColumnDescription = "角色名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string RoleName { get; set; }
        /// <summary>
        /// 排序序号
        ///</summary>
        [DisplayName("排序序号")]
        [SugarColumn(ColumnName = "sort_border", IsNullable = true, Length = 10, ColumnDescription = "排序序号", DefaultValue = "", ColumnDataType = "varchar")]
        public string SortBorder { get; set; }
        /// <summary>
        /// 角色级别
        ///</summary>
        [DisplayName("角色级别")]
        [SugarColumn(ColumnName = "tree_level", ColumnDescription = "角色级别", DefaultValue = "0", ColumnDataType = "int")]
        public int TreeLevel { get; set; }
        /// <summary>
        /// 上级角色ID
        ///</summary>
        [DisplayName("上级角色ID")]
        [SugarColumn(ColumnName = "parent_id", ColumnDescription = "上级角色ID", DefaultValue = "0", ColumnDataType = "int")]
        public int ParentId { get; set; }
        /// <summary>
        /// 角色名称(全)
        ///</summary>
        [DisplayName("角色名称(全)")]
        [SugarColumn(ColumnName = "full_name", IsNullable = true, Length = 400, ColumnDescription = "角色名称(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullName { get; set; }
        /// <summary>
        /// 角色ID(全)
        ///</summary>
        [DisplayName("角色ID(全)")]
        [SugarColumn(ColumnName = "full_code", IsNullable = true, Length = 200, ColumnDescription = "角色ID(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullCode { get; set; }
        /// <summary>
        /// 角色描述
        ///</summary>
        [DisplayName("角色描述")]
        [SugarColumn(ColumnName = "role_describe", IsNullable = true, Length = 50, ColumnDescription = "角色描述", DefaultValue = "", ColumnDataType = "varchar")]
        public string RoleDescribe { get; set; }
        /// <summary>
        /// 是否有子集
        ///</summary>
        [DisplayName("是否有子集")]
        [SugarColumn(ColumnName = "has_child", Length = 1, ColumnDescription = "是否有子集", DefaultValue = "0", ColumnDataType = "bit")]
        public bool HasChild { get; set; }
    }
}