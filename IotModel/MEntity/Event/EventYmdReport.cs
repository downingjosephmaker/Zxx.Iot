using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 野猫墩抄表
    ///</summary>
    [DisplayName("野猫墩抄表")]
    [SugarTable(TableName = "event_ymd_report", TableDescription = "野猫墩抄表", IsDisabledUpdateAll = true)]
    public class EventYmdReport
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
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
        /// 记录时间
        ///</summary>
        [DisplayName("记录时间")]
        [SugarColumn(ColumnName = "event_time", IsNullable = true, Length = 20, ColumnDescription = "记录时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string EventTime { get; set; }
        /// <summary>
        /// 文件名称
        ///</summary>
        [DisplayName("文件名称")]
        [SugarColumn(ColumnName = "file_name", Length = 100, ColumnDescription = "文件名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string FileName { get; set; }
        /// <summary>
        /// 文件路径
        ///</summary>
        [DisplayName("文件路径")]
        [SugarColumn(ColumnName = "file_path", IsNullable = true, Length = 300, ColumnDescription = "文件路径", DefaultValue = "", ColumnDataType = "varchar")]
        public string FilePath { get; set; }
    }
}