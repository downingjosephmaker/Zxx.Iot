using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备类型表
    ///</summary>
    [DisplayName("设备类型表")]
    [EntityCache]
    [SugarTable(TableName = "device_type", TableDescription = "设备类型信息", IsDisabledUpdateAll = true)]
    public class DeviceType : BaseEntity
    {
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        [SugarColumn(ColumnName = "type_code", IsPrimaryKey = true, Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string TypeCode { get; set; }
        /// <summary>
		/// 设备类型名称
		///</summary>
		[DisplayName("设备类型名称")]
        [SugarColumn(ColumnName = "type_name", IsNullable = true, Length = 50, ColumnDescription = "设备类型名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string TypeName { get; set; }
        /// <summary>
        /// 排序序号
        ///</summary>
        [DisplayName("排序序号")]
        [SugarColumn(ColumnName = "sort_border", IsNullable = true, Length = 10, ColumnDescription = "排序序号", DefaultValue = "", ColumnDataType = "varchar")]
        public string SortBorder { get; set; }
        /// <summary>
        /// 类型级别
        ///</summary>
        [DisplayName("类型级别")]
        [SugarColumn(ColumnName = "tree_level", ColumnDescription = "类型级别", DefaultValue = "0", ColumnDataType = "int")]
        public int TreeLevel { get; set; }
        /// <summary>
        /// 上级类型编码
        ///</summary>
        [DisplayName("上级类型编码")]
        [SugarColumn(ColumnName = "parent_id", IsNullable = true, Length = 30, ColumnDescription = "上级类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string ParentId { get; set; }
        /// <summary>
        /// 类型名称(全)
        ///</summary>
        [DisplayName("类型名称(全)")]
        [SugarColumn(ColumnName = "full_name", IsNullable = true, Length = 400, ColumnDescription = "类型名称(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullName { get; set; }
        /// <summary>
        /// 类型ID(全)
        ///</summary>
        [DisplayName("类型ID(全)")]
        [SugarColumn(ColumnName = "full_code", IsNullable = true, Length = 200, ColumnDescription = "类型ID(全)", DefaultValue = "", ColumnDataType = "varchar")]
        public string FullCode { get; set; }
        /// <summary>
        /// 是否有子集
        ///</summary>
        [DisplayName("是否有子集")]
        [SugarColumn(ColumnName = "has_child", Length = 1, ColumnDescription = "是否有子集", DefaultValue = "0", ColumnDataType = "bit")]
        public bool HasChild { get; set; }
        /// <summary>
        /// 是否可用(0:否1:是)
        ///</summary>
        [DisplayName("是否可用(0:否1:是)")]
        [SugarColumn(ColumnName = "is_enable", Length = 1, ColumnDescription = "是否可用(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsEnable { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_DeviceType))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}