using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 建筑表
    ///</summary>
    [DisplayName("建筑表")]
    [EntityCache]
    [SugarTable(TableName = "build_info", TableDescription = "建筑表", IsDisabledUpdateAll = true)]
    public class BuildInfo : BaseEntity, IUnitEntity
    {
        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        [SugarColumn(ColumnName = "build_id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "建筑ID", DefaultValue = "0", ColumnDataType = "int")]
        public int BuildId { get; set; }
        /// <summary>
        /// 建筑名称
        ///</summary>
        [DisplayName("建筑名称")]
        [SugarColumn(ColumnName = "build_name", Length = 100, ColumnDescription = "建筑名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string BuildName { get; set; }
        /// <summary>
        /// 建筑编号
        ///</summary>
        [DisplayName("建筑编号")]
        [SugarColumn(ColumnName = "build_code", IsNullable = true, Length = 20, ColumnDescription = "建筑编号", DefaultValue = "", ColumnDataType = "varchar")]
        public string BuildCode { get; set; }
        /// <summary>
        /// 排序序号
        ///</summary>
        [DisplayName("排序序号")]
        [SugarColumn(ColumnName = "sort_border", IsNullable = true, Length = 10, ColumnDescription = "排序序号", DefaultValue = "", ColumnDataType = "varchar")]
        public string SortBorder { get; set; }
        /// <summary>
        /// 建筑级别
        ///</summary>
        [DisplayName("建筑级别")]
        [SugarColumn(ColumnName = "tree_level", ColumnDescription = "建筑级别", DefaultValue = "0", ColumnDataType = "int")]
        public int TreeLevel { get; set; }
        /// <summary>
        /// 上级建筑ID
        ///</summary>
        [DisplayName("上级建筑ID")]
        [SugarColumn(ColumnName = "parent_id", ColumnDescription = "上级建筑ID", DefaultValue = "0", ColumnDataType = "int")]
        public int ParentId { get; set; }
        /// <summary>
        /// 建筑名称(全)
        ///</summary>
        [DisplayName("建筑名称(全)")]
        [SugarColumn(ColumnName = "full_name", IsNullable = true, Length = 400, ColumnDescription = "建筑名称(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullName { get; set; }
        /// <summary>
        /// 建筑ID(全)
        ///</summary>
        [DisplayName("建筑ID(全)")]
        [SugarColumn(ColumnName = "full_code", IsNullable = true, Length = 200, ColumnDescription = "建筑ID(全)", DefaultValue = "", ColumnDataType = "varchar")]
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