using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 记录基础表
    /// </summary>
    public class EventBase : ITenantEntity
    {
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
        /// <summary>
        /// 单位名称
        ///</summary>
        [DisplayName("单位名称")]
        [SugarColumn(ColumnName = "unit_name", IsNullable = true, Length = 50, ColumnDescription = "单位名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string UnitName { get; set; }
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
