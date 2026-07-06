using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 数据清理时间设置
    /// </summary>
    [DisplayName("数据清理时间设置")]
    [EntityCache]
    [SugarTable(TableName = "sys_clean_config", TableDescription = "数据清理时间设置", IsDisabledUpdateAll = true)]
    public class SysCleanConfig : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }

        /// <summary>
        /// 清理名称
        /// </summary>
        [DisplayName("清理名称")]
        [SugarColumn(ColumnName = "clean_name", ColumnDescription = "清理名称", Length = 50, ColumnDataType = "varchar")]
        public string CleanName { get; set; }

        /// <summary>
        /// 数据类型编码
        /// </summary>
        [DisplayName("数据类型编码")]
        [SugarColumn(ColumnName = "data_code", ColumnDescription = "数据类型编码", Length = 50, ColumnDataType = "varchar")]
        public string DataCode { get; set; }

        /// <summary>
        /// 保留天数
        /// </summary>
        [DisplayName("保留天数")]
        [SugarColumn(ColumnName = "retention_days", ColumnDescription = "保留天数", DefaultValue = "30", ColumnDataType = "int")]
        public int RetentionDays { get; set; }

        /// <summary>
        /// 是否启用自动清理
        /// </summary>
        [DisplayName("是否启用自动清理")]
        [SugarColumn(ColumnName = "is_auto_cleanup", ColumnDescription = "是否启用自动清理", DefaultValue = "1", Length = 1, ColumnDataType = "bit")]
        public bool IsAutoCleanup { get; set; }

        /// <summary>
        /// 最后清理时间
        /// </summary>
        [DisplayName("最后清理时间")]
        [SugarColumn(ColumnName = "last_cleanup_time", IsNullable = true, ColumnDescription = "最后清理时间", Length = 20, ColumnDataType = "varchar")]
        public string LastCleanupTime { get; set; }
    }
}