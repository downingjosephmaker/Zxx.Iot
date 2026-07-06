using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备参数表
    ///</summary>
    [DisplayName("设备参数表")]
    [SugarTable(TableName = "device_param", TableDescription = "设备参数表", IsDisabledUpdateAll = true)]
    public class DeviceParam : BaseEntity
    {
        /// <summary>
        /// 设备主键
        ///</summary>
        [DisplayName("设备主键")]
        [SugarColumn(ColumnName = "device_id", IsPrimaryKey = true, ColumnDescription = "设备主键", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        [SugarColumn(ColumnName = "device_type_code", Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        [SugarColumn(ColumnName = "build_id", ColumnDescription = "设备ID", DefaultValue = "0", ColumnDataType = "int")]
        public int BuildId { get; set; }
        /// <summary>
        /// 部门ID
        ///</summary>
        [DisplayName("部门ID")]
        [SugarColumn(ColumnName = "dept_id", ColumnDescription = "部门ID", DefaultValue = "0", ColumnDataType = "int")]
        public int DeptId { get; set; }
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
        [JsonField(typeof(Expand_DeviceParam))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}
