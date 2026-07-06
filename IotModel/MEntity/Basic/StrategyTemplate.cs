using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 策略模板表
    ///</summary>
    [DisplayName("策略模板表")]
    [SugarTable(TableName = "strategy_template", TableDescription = "策略模板表", IsDisabledUpdateAll = true)]
    public class StrategyTemplate : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 设备类型编码
        ///</summary>
        [DisplayName("设备类型编码")]
        [SugarColumn(ColumnName = "device_type_code", Length = 30, ColumnDescription = "设备类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeCode { get; set; }
        /// <summary>
        /// 设备全类型编码
        ///</summary>
        [DisplayName("设备全类型编码")]
        [SugarColumn(ColumnName = "device_type_full_code", Length = 200, ColumnDescription = "设备全类型编码", DefaultValue = "", ColumnDataType = "varchar")]
        public string DeviceTypeFullCode { get; set; }
        /// <summary>
        /// 模板名称
        ///</summary>
        [DisplayName("模板名称")]
        [SugarColumn(ColumnName = "strategy_name", IsNullable = true, Length = 50, ColumnDescription = "模板名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string StrategyName { get; set; }
        /// <summary>
        /// 策略内容
        ///</summary>
        [DisplayName("策略内容")]
        [SugarColumn(ColumnName = "strategy_json", IsNullable = true, ColumnDescription = "策略内容", ColumnDataType = "text")]
        public string StrategyJson { get; set; }

    }
}