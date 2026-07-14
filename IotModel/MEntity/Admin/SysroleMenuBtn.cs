using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 角色菜单按钮权限
    ///</summary>
    [DisplayName("角色菜单按钮权限")]
    [EntityCache]
    [SugarTable(TableName = "sys_role_menu_btn", TableDescription = "角色菜单按钮权限", IsDisabledUpdateAll = true)]
    public class SysRoleMenuBtn : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// 租户ID(随所属角色隔离,0=平台共享角色的授权)
        ///</summary>
        [DisplayName("租户ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 角色ID
        ///</summary>
        [DisplayName("角色ID")]
        [SugarColumn(ColumnName = "role_id", ColumnDescription = "角色ID", DefaultValue = "0", ColumnDataType = "int")]
        public int RoleId { get; set; }
        /// <summary>
        /// 菜单ID
        ///</summary>
        [DisplayName("菜单ID")]
        [SugarColumn(ColumnName = "menu_id", Length = 20, ColumnDescription = "菜单ID", DefaultValue = "0", ColumnDataType = "varchar")]
        public string MenuId { get; set; }
        /// <summary>
        /// 按钮ID
        ///</summary>
        [DisplayName("按钮ID")]
        [SugarColumn(ColumnName = "button_id", ColumnDescription = "按钮ID", DefaultValue = "0", ColumnDataType = "int")]
        public int ButtonId { get; set; }
    }
}