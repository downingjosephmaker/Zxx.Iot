using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 导航菜单
    ///</summary>
    [DisplayName("导航菜单")]
    [EntityCache]
    [SugarTable(TableName = "sys_menu", TableDescription = "导航菜单", IsDisabledUpdateAll = true)]
    public class SysMenu : BaseEntity
    {
        /// <summary>
        /// 菜单ID
        ///</summary>
        [DisplayName("菜单ID")]
        [SugarColumn(ColumnName = "menu_id", IsPrimaryKey = true, Length = 10, ColumnDescription = "菜单ID", DefaultValue = "", ColumnDataType = "varchar")]
        public string MenuId { get; set; }
        /// <summary>
        /// 菜单编码
        ///</summary>
        [DisplayName("菜单编码")]
        [SugarColumn(ColumnName = "menu_code", Length = 30, ColumnDescription = "菜单编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string MenuCode { get; set; }
        /// <summary>
        /// 菜单名称
        ///</summary>
        [DisplayName("菜单名称")]
        [SugarColumn(ColumnName = "menu_name", Length = 50, ColumnDescription = "菜单名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string MenuName { get; set; }
        /// <summary>
        /// 上级ID
        ///</summary>
        [DisplayName("上级ID")]
        [SugarColumn(ColumnName = "parent_id", Length = 20, ColumnDescription = "上级ID", DefaultValue = "0", ColumnDataType = "varchar")]
        public string ParentId { get; set; }
        /// <summary>
        /// 菜单Url
        ///</summary>
        [DisplayName("菜单Url")]
        [SugarColumn(ColumnName = "menu_url", IsNullable = true, Length = 200, ColumnDescription = "菜单Url", DefaultValue = "", ColumnDataType = "varchar")]
        public string MenuUrl { get; set; }
        /// <summary>
        /// 图标样式
        ///</summary>
        [DisplayName("图标样式")]
        [SugarColumn(ColumnName = "menu_icon", IsNullable = true, Length = 100, ColumnDescription = "图标样式", DefaultValue = "", ColumnDataType = "varchar")]
        public string MenuIcon { get; set; }
        /// <summary>
        /// 是否显示菜单中(1:是 0:否)
        ///</summary>
        [DisplayName("是否显示菜单中(1:是 0:否)")]
        [SugarColumn(ColumnName = "is_show_link", ColumnDescription = "是否显示菜单中(1:是 0:否)", DefaultValue = "1", ColumnDataType = "int")]
        public int IsShowLink { get; set; }
        /// <summary>
        /// 排序序号
        ///</summary>
        [DisplayName("排序序号")]
        [SugarColumn(ColumnName = "sort_border", IsNullable = true, Length = 10, ColumnDescription = "排序序号", DefaultValue = "", ColumnDataType = "varchar")]
        public string SortBorder { get; set; }
        /// <summary>
        /// 菜单级别
        ///</summary>
        [DisplayName("菜单级别")]
        [SugarColumn(ColumnName = "tree_level", ColumnDescription = "菜单级别", DefaultValue = "0", ColumnDataType = "int")]
        public int TreeLevel { get; set; }
        /// <summary>
        /// 菜单名称(全)
        ///</summary>
        [DisplayName("菜单名称(全)")]
        [SugarColumn(ColumnName = "full_name", IsNullable = true, Length = 400, ColumnDescription = "菜单名称(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullName { get; set; }
        /// <summary>
        /// 菜单ID(全)
        ///</summary>
        [DisplayName("菜单ID(全)")]
        [SugarColumn(ColumnName = "full_code", IsNullable = true, Length = 200, ColumnDescription = "菜单ID(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullCode { get; set; }
        /// <summary>
        /// 是否有子集
        ///</summary>
        [DisplayName("是否有子集")]
        [SugarColumn(ColumnName = "has_child", Length = 1, ColumnDescription = "是否有子集", DefaultValue = "0", ColumnDataType = "bit")]
        public bool HasChild { get; set; }
    }
}