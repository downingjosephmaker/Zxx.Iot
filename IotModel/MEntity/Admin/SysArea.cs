using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 行政区划表
    ///</summary>
    [DisplayName("行政区划表")]
    [EntityCache]
    [SugarTable(TableName = "sys_area", TableDescription = "行政区划表", IsDisabledUpdateAll = true)]
    public class SysArea
    {
        /// <summary>
        /// 行政区划Id
        ///</summary>
        [DisplayName("行政区划Id")]
        [SugarColumn(ColumnName = "area_id", IsPrimaryKey = true, IsNullable = false, Length = 10, ColumnDescription = "行政区划Id", DefaultValue = "", ColumnDataType = "varchar")]
        public string AreaId { get; set; }
        /// <summary>
        /// 行政区划名称
        ///</summary>
        [DisplayName("行政区划名称")]
        [SugarColumn(ColumnName = "area_name", IsNullable = true, Length = 50, ColumnDescription = "行政区划名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string AreaName { get; set; }
        /// <summary>
        /// 区域名称
        ///</summary>
        [DisplayName("区域名称")]
        [SugarColumn(ColumnName = "division_name", IsNullable = true, Length = 20, ColumnDescription = "区域名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DivisionName { get; set; }
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
        /// 上级ID
        ///</summary>
        [DisplayName("上级ID")]
        [SugarColumn(ColumnName = "parent_id", Length = 10, ColumnDescription = "上级ID", DefaultValue = "0", ColumnDataType = "varchar")]
        public string ParentId { get; set; }
        /// <summary>
        /// 行政区划名称(全)
        ///</summary>
        [DisplayName("行政区划名称(全)")]
        [SugarColumn(ColumnName = "full_name", IsNullable = true, Length = 400, ColumnDescription = "行政区划名称(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullName { get; set; }
        /// <summary>
        /// 行政区划ID(全)
        ///</summary>
        [DisplayName("行政区划ID(全)")]
        [SugarColumn(ColumnName = "full_code", IsNullable = true, Length = 200, ColumnDescription = "行政区划ID(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullCode { get; set; }
        /// <summary>
        /// 是否有子集
        ///</summary>
        [DisplayName("是否有子集")]
        [SugarColumn(ColumnName = "has_child", Length = 1, ColumnDescription = "是否有子集", DefaultValue = "0", ColumnDataType = "bit")]
        public bool HasChild { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_SysArea))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}