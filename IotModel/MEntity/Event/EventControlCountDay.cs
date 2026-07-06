using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 控制|策略次数日表
    ///</summary>
    [DisplayName("控制|策略次数日表")]
    [SplitTable(SplitType.Month, typeof(SnowSplitService))]
    [SugarTable(TableName = "event_control_count_day", TableDescription = "控制|策略次数日表", IsDisabledUpdateAll = true)]
    public class EventControlCountDay : EventBase
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
        /// 运行下发次数
        ///</summary>
        [DisplayName("运行下发次数")]
        [SugarColumn(ColumnName = "cnt_ctrl", Length = 20, ColumnDescription = "运行下发次数", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntCtrl { get; set; }
        /// <summary>
        /// 策略下发次数
        ///</summary>
        [DisplayName("策略下发次数")]
        [SugarColumn(ColumnName = "cnt_stg", Length = 20, ColumnDescription = "策略下发次数", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntStg { get; set; }
        /// <summary>
        /// 参数下发次数
        ///</summary>
        [DisplayName("参数下发次数")]
        [SugarColumn(ColumnName = "opt_pam", Length = 20, ColumnDescription = "参数下发次数", DefaultValue = "0", ColumnDataType = "bigint")]
        public long CntPam { get; set; }
        /// <summary>
        /// 发生时段(1:(8-12)时 2:(12-18)时 3:(18-22)时)
        ///</summary>
        [DisplayName("发生时段(1:(8-12)时 2:(12-18)时 3:(18-22)时)")]
        [SugarColumn(ColumnName = "ctrl_time", Length = 11, ColumnDescription = "发生时段(1:(8-12)时 2:(12-18)时 3:(18-22)时)", DefaultValue = "0", ColumnDataType = "bigint")]
        public int CtrlTime { get; set; }
    }
}