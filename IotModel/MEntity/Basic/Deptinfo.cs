using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 组织表
    ///</summary>
    [DisplayName("组织表")]
    [EntityCache]
    [SugarTable(TableName = "dept_info", TableDescription = "组织表", IsDisabledUpdateAll = true)]
    public class DeptInfo : BaseEntity, IUnitEntity
    {
        /// <summary>
        /// 组织ID
        ///</summary>
        [DisplayName("组织ID")]
        [SugarColumn(ColumnName = "dept_id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "组织ID", DefaultValue = "0", ColumnDataType = "int")]
        public int DeptId { get; set; }
        /// <summary>
        /// 组织名称
        ///</summary>
        [DisplayName("组织名称")]
        [SugarColumn(ColumnName = "dept_name", IsNullable = true, Length = 100, ColumnDescription = "组织名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeptName { get; set; }
        /// <summary>
        /// 组织编号
        ///</summary>
        [DisplayName("组织编号")]
        [SugarColumn(ColumnName = "dept_code", IsNullable = true, Length = 50, ColumnDescription = "组织编号", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeptCode { get; set; }
        /// <summary>
        /// 排序序号
        ///</summary>
        [DisplayName("排序序号")]
        [SugarColumn(ColumnName = "sort_border", IsNullable = true, Length = 10, ColumnDescription = "排序序号", DefaultValue = "", ColumnDataType = "varchar")]
        public string SortBorder { get; set; }
        /// <summary>
        /// 组织级别
        ///</summary>
        [DisplayName("组织级别")]
        [SugarColumn(ColumnName = "tree_level", ColumnDescription = "组织级别", DefaultValue = "0", ColumnDataType = "int")]
        public int TreeLevel { get; set; }
        /// <summary>
        /// 上级组织ID
        ///</summary>
        [DisplayName("上级组织ID")]
        [SugarColumn(ColumnName = "parent_id", ColumnDescription = "上级组织ID", DefaultValue = "0", ColumnDataType = "int")]
        public int ParentId { get; set; }
        /// <summary>
        /// 组织名称(全)
        ///</summary>
        [DisplayName("组织名称(全)")]
        [SugarColumn(ColumnName = "full_name", IsNullable = true, Length = 400, ColumnDescription = "组织名称(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullName { get; set; }
        /// <summary>
        /// 组织ID(全)
        ///</summary>
        [DisplayName("组织ID(全)")]
        [SugarColumn(ColumnName = "full_code", IsNullable = true, Length = 200, ColumnDescription = "组织ID(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullCode { get; set; }
        /// <summary>
        /// 是否有子集
        ///</summary>
        [DisplayName("是否有子集")]
        [SugarColumn(ColumnName = "has_child", Length = 1, ColumnDescription = "是否有子集", DefaultValue = "0", ColumnDataType = "bit")]
        public bool HasChild { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_DeptBuild))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}