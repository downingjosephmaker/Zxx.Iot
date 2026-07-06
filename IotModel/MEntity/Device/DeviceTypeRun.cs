using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 现有设备大类
    /// </summary>
    [DisplayName("现有设备大类")]
    [EntityCache]
    [SugarTable(TableName = "device_type_run", TableDescription = "现有设备大类", IsDisabledUpdateAll = true)]
    public class DeviceTypeRun : IUnitEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint", IsIdentity = true)]
        public long SnowId { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }

        /// <summary>
        /// 设备大类编码
        ///</summary>
        [DisplayName("设备大类编码")]
        [SugarColumn(ColumnName = "device_type_code", Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备大类名称
        ///</summary>
        [DisplayName("设备大类名称")]
        [SugarColumn(ColumnName = "device_type_name", Length = 30, ColumnDescription = "设备大类名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeName { get; set; }
        /// <summary>
        /// 类型分组(|隔开)
        ///</summary>
        [DisplayName("类型分组(|隔开)")]
        [SugarColumn(ColumnName = "menu_code", Length = 300, ColumnDescription = "类型分组(|隔开)", DefaultValue = "", ColumnDataType = "varchar")]
        public string MenuCode { get; set; }
    }
}
