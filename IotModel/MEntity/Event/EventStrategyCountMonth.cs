using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 策略模型次数月表
    ///</summary>
    [DisplayName("策略模型次数月表")]
    [SugarTable(TableName = "event_strategy_count_month", TableDescription = "策略模型次数月表", IsDisabledUpdateAll = true)]
    public class EventStrategyCountMonth : EventBase
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
		/// 策略模式-调温
		///</summary>
		[DisplayName("策略模式-调温")]
        [SugarColumn(ColumnName = "cnt_mode0", Length = 20, ColumnDescription = "策略模式-调温", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode0 { get; set; }
        /// <summary>
        /// 策略模式-人感
        ///</summary>
        [DisplayName("策略模式-人感")]
        [SugarColumn(ColumnName = "cnt_mode1", Length = 20, ColumnDescription = "策略模式-人感", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode1 { get; set; }
        /// <summary>
        /// 策略模式-温度
        ///</summary>
        [DisplayName("策略模式-温度")]
        [SugarColumn(ColumnName = "cnt_mode2", Length = 20, ColumnDescription = "策略模式-温度", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode2 { get; set; }
        /// <summary>
        /// 策略模式-时间
        ///</summary>
        [DisplayName("策略模式-时间")]
        [SugarColumn(ColumnName = "cnt_mode3", Length = 20, ColumnDescription = "策略模式-时间", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode3 { get; set; }
        /// <summary>
        /// 策略模式-手动
        ///</summary>
        [DisplayName("策略模式-手动")]
        [SugarColumn(ColumnName = "cnt_mode4", Length = 20, ColumnDescription = "策略模式-手动", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode4 { get; set; }
        /// <summary>
        /// 策略模式-计量
        ///</summary>
        [DisplayName("策略模式-计量")]
        [SugarColumn(ColumnName = "cnt_mode5", Length = 20, ColumnDescription = "策略模式-计量", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode5 { get; set; }
        /// <summary>
        /// 策略模式-断电
        ///</summary>
        [DisplayName("策略模式-断电")]
        [SugarColumn(ColumnName = "cnt_mode7", Length = 20, ColumnDescription = "策略模式-断电", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode7 { get; set; }
        /// <summary>
        /// 策略模式-临时
        ///</summary>
        [DisplayName("策略模式-临时")]
        [SugarColumn(ColumnName = "cnt_mode9", Length = 20, ColumnDescription = "策略模式-临时", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode9 { get; set; }
        /// <summary>
        /// 策略模式-定时关机
        ///</summary>
        [DisplayName("策略模式-定时关机")]
        [SugarColumn(ColumnName = "cnt_mode10", Length = 20, ColumnDescription = "策略模式-定时关机", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode10 { get; set; }
        /// <summary>
        /// 策略模式-制热
        ///</summary>
        [DisplayName("策略模式-制热")]
        [SugarColumn(ColumnName = "cnt_mode11", Length = 20, ColumnDescription = "策略模式-制热", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntMode11 { get; set; }
    }
}