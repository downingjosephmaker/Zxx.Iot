using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 设备关联表
    ///</summary>
    [DisplayName("设备关联表")]
    [EntityCache]
    [SugarTable(TableName = "device_relevance", TableDescription = "设备关联表", IsDisabledUpdateAll = true)]
    public class DeviceRelevance : BaseEntity, IUnitEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 设备ID
        ///</summary>
        [DisplayName("设备ID")]
        [SugarColumn(ColumnName = "device_id", ColumnDescription = "设备主键", DefaultValue = "0", ColumnDataType = "int")]
        public int DeviceId { get; set; }
        /// <summary>
        /// 设备名称
        ///</summary>
        [DisplayName("设备名称")]
        [SugarColumn(ColumnName = "device_name", Length = 50, ColumnDescription = "设备名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceName { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        [SugarColumn(ColumnName = "device_type_code", Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 关联设备ID
        ///</summary>
        [DisplayName("关联设备ID")]
        [SugarColumn(ColumnName = "relevance_id", ColumnDescription = "关联设备ID", DefaultValue = "0", ColumnDataType = "int")]
        public int RelevanceId { get; set; }
        /// <summary>
        /// 关联设备名称
        ///</summary>
        [DisplayName("关联设备名称")]
        [SugarColumn(ColumnName = "relevance_name", Length = 50, ColumnDescription = "关联设备名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string RelevanceName { get; set; }
        /// <summary>
        /// 关联设备类型编码
        ///</summary>
        [DisplayName("关联设备类型编码")]
        [SugarColumn(ColumnName = "relevance_type_code", Length = 30, ColumnDescription = "关联设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string RelevanceTypeCode { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }

    }
}
