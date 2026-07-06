using Newtonsoft.Json;
using SqlSugar;
using System;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 工作日信息表
    /// </summary>
    [DisplayName("工作日信息表")]
    [SugarTable(TableName = "workday_info", TableDescription = "工作日信息表", IsDisabledUpdateAll = true)]
    public class WorkdayInfo
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 年份
        ///</summary>
        [DisplayName("年份")]
        [SugarColumn(ColumnName = "work_year", ColumnDescription = "年份", DefaultValue = "0", ColumnDataType = "int")]
        public int WorkYear { get; set; }
        /// <summary>
        /// 日期
        /// </summary>
        [DisplayName("日期")]
        [SugarColumn(ColumnName = "date", IsNullable = false, ColumnDescription = "日期", ColumnDataType = "date")]
        public DateTime Date { get; set; }
        /// <summary>
        /// 是否上班日
        /// </summary>
        [DisplayName("是否上班日")]
        [SugarColumn(ColumnName = "is_workday", Length = 1, ColumnDescription = "是否上班日", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsWorkday { get; set; }
        /// <summary>
        /// 是否节假日
        /// </summary>
        [DisplayName("是否节假日")]
        [SugarColumn(ColumnName = "is_holiday", Length = 1, ColumnDescription = "是否节假日", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsHoliday { get; set; }
        /// <summary>
        /// 是否周末
        /// </summary>
        [DisplayName("是否周末")]
        [SugarColumn(ColumnName = "is_weekday", Length = 1, ColumnDescription = "是否周末", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsWeekday { get; set; }
    }
}