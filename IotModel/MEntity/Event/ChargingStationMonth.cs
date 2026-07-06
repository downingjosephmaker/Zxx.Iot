using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 充电桩统计月表(叉车/堆高车/汽车)
    ///</summary>
    [DisplayName("充电桩统计日表(叉车/堆高车/汽车)")]
    [SplitTable(SplitType.Year, typeof(SnowSplitService))]
    [SugarTable(TableName = "charging_station_month", TableDescription = "充电桩统计月表(叉车/堆高车/汽车)", IsDisabledUpdateAll = true)]
    public class ChargingStationMonth : EventBase
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [SplitField] //分表字段
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 总次数
        ///</summary>
        [DisplayName("总次数")]
        [SugarColumn(ColumnName = "charging_count", ColumnDescription = "总次数", DefaultValue = "0", ColumnDataType = "int")]
        public int ChargingCount { get; set; }
        /// <summary>
        /// 总时长(分)
        ///</summary>
        [DisplayName("总时长(分)")]
        [SugarColumn(ColumnName = "charging_duration", ColumnDescription = "总时长(分)", DefaultValue = "0", ColumnDataType = "int")]
        public int ChargingDuration { get; set; }
        /// <summary>
        /// 总能耗
        /// </summary>
        [DisplayName("总能耗")]
        [SugarColumn(ColumnName = "charging_energy", Length = 18, DecimalDigits = 3, ColumnDescription = "总能耗", DefaultValue = "0", ColumnDataType = "decimal")]
        public decimal ChargingEnergy { get; set; }
        /// <summary>
        /// 拓展属性(json)
        ///</summary>
        [DisplayName("拓展属性(json)")]
        [JsonField(typeof(Expand_ChargingStationDay))]
        [SugarColumn(ColumnName = "expand_json", IsNullable = true, ColumnDescription = "拓展属性(json)", ColumnDataType = "text")]
        public string ExpandJson { get; set; }
    }
}