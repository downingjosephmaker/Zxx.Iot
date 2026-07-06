using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 单位基本信息
    ///</summary>
    [DisplayName("单位基本信息")]
    [EntityCache]
    [SugarTable(TableName = "basicunit_info", TableDescription = "单位基本信息", IsDisabledUpdateAll = true)]
    public class BasicunitInfo : BaseEntity
    {
        /// <summary>
        /// 主键
        ///</summary>
        [DisplayName("主键")]
        [SugarColumn(ColumnName = "unit_id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "主键", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }
        /// <summary>
        /// 单位名称
        ///</summary>
        [DisplayName("单位名称")]
        [SugarColumn(ColumnName = "unit_name", IsNullable = true, Length = 50, ColumnDescription = "单位名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string UnitName { get; set; }
        /// <summary>
        /// 单位编码
        ///</summary>
        [DisplayName("单位编码")]
        [SugarColumn(ColumnName = "unit_code", IsNullable = true, Length = 50, ColumnDescription = "单位编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string UnitCode { get; set; }
        /// <summary>
        /// 区域Id
        ///</summary>
        [DisplayName("区域Id")]
        [SugarColumn(ColumnName = "area_id", IsNullable = true, Length = 100, ColumnDescription = "区域Id", DefaultValue = "", ColumnDataType = "varchar")]
        public string AreaId { get; set; }
        /// <summary>
        /// 区域名称
        ///</summary>
        [DisplayName("区域名称")]
        [SugarColumn(ColumnName = "area_name", IsNullable = true, Length = 200, ColumnDescription = "区域名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string AreaName { get; set; }
        /// <summary>
        /// 建筑面积
        ///</summary>
        [DisplayName("建筑面积")]
        [SugarColumn(ColumnName = "structure_area", IsNullable = true, Length = 18, DecimalDigits = 2, ColumnDescription = "建筑面积", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal StructureArea { get; set; }
        /// <summary>
        /// 使用面积
        ///</summary>
        [DisplayName("使用面积")]
        [SugarColumn(ColumnName = "usable_area", IsNullable = true, Length = 18, DecimalDigits = 2, ColumnDescription = "使用面积", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal UsableArea { get; set; }
        /// <summary>
        /// 总人数
        ///</summary>
        [DisplayName("总人数")]
        [SugarColumn(ColumnName = "total_people", ColumnDescription = "总人数", DefaultValue = "0", ColumnDataType = "int")]
        public int TotalPeople { get; set; }
        /// <summary>
        /// 单位地址
        ///</summary>
        [DisplayName("单位地址")]
        [SugarColumn(ColumnName = "unit_address", IsNullable = true, Length = 255, ColumnDescription = "单位地址", DefaultValue = "", ColumnDataType = "varchar")]
        public string UnitAddress { get; set; }
        /// <summary>
        /// 备注
        ///</summary>
        [DisplayName("备注")]
        [SugarColumn(ColumnName = "unit_remark", IsNullable = true, Length = 300, ColumnDescription = "备注", DefaultValue = "", ColumnDataType = "varchar")]
        public string UnitRemark { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_BasicunitInfo))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}