using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 记录基础表
    /// </summary>
    public class EventBase : IUnitEntity
    {
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }
        /// <summary>
        /// 单位名称
        ///</summary>
        [DisplayName("单位名称")]
        [SugarColumn(ColumnName = "unit_name", IsNullable = true, Length = 50, ColumnDescription = "单位名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string UnitName { get; set; }
        /// <summary>
        /// 建筑ID
        ///</summary>
        [DisplayName("建筑ID")]
        [SugarColumn(ColumnName = "build_id", ColumnDescription = "建筑ID", DefaultValue = "0", ColumnDataType = "int")]
        public int BuildId { get; set; }
        /// <summary>
        /// 建筑名称
        ///</summary>
        [DisplayName("建筑名称")]
        [SugarColumn(ColumnName = "build_name", IsNullable = true, Length = 300, ColumnDescription = "建筑名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string BuildName { get; set; }
        /// <summary>
        /// 部门ID
        ///</summary>
        [DisplayName("部门ID")]
        [SugarColumn(ColumnName = "dept_id", ColumnDescription = "部门ID", DefaultValue = "0", ColumnDataType = "int")]
        public int DeptId { get; set; }
        /// <summary>
        /// 部门名称
        ///</summary>
        [DisplayName("部门名称")]
        [SugarColumn(ColumnName = "dept_name", IsNullable = true, Length = 300, ColumnDescription = "部门名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeptName { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        [SugarColumn(ColumnName = "device_type_code", IsNullable = true, Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备类型名称
        ///</summary>
        [DisplayName("设备类型名称")]
        [SugarColumn(ColumnName = "device_type_name", IsNullable = true, Length = 300, ColumnDescription = "设备类型名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeName { get; set; }
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        [SugarColumn(ColumnName = "device_id", ColumnDescription = "设备ID", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备名称
        ///</summary>
        [DisplayName("设备名称")]
        [SugarColumn(ColumnName = "device_name", IsNullable = true, Length = 300, ColumnDescription = "设备名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceName { get; set; }
        /// <summary>
        /// 记录时间
        ///</summary>
        [DisplayName("记录时间")]
        [SugarColumn(ColumnName = "event_time", IsNullable = true, Length = 20, ColumnDescription = "记录时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string EventTime { get; set; }
    }
}
