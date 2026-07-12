using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备参数表
    ///</summary>
    [DisplayName("设备参数表")]
    [SugarTable(TableName = "device_param", TableDescription = "设备参数表", IsDisabledUpdateAll = true)]
    public class DeviceParam : BaseEntity, ITenantEntity
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
        /// 租户ID
        ///</summary>
        [DisplayName("租户ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_DeviceParam))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}
