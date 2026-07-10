using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 空调舒适度
    ///</summary>
    [DisplayName("空调舒适度")]
    [EntityCache]
    [SugarTable(TableName = "device_comfort", TableDescription = "空调舒适度", IsDisabledUpdateAll = true)]
    public class DeviceComfort : BaseEntity, ITenantEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
		/// 季节名称
		///</summary>
		[DisplayName("季节名称")]
        [SugarColumn(ColumnName = "comfort_name", IsNullable = true, Length = 50, ColumnDescription = "季节名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string ComfortName { get; set; }
        /// <summary>
        /// 环境湿度
        ///</summary>
        [DisplayName("环境湿度")]
        [SugarColumn(ColumnName = "envir_humidity", ColumnDescription = "环境湿度", DefaultValue = "0", ColumnDataType = "int")]
        public int EnvirHumidity { get; set; }
        /// <summary>
        /// 舒适度公式
        ///</summary>
        [DisplayName("舒适度公式")]
        [SugarColumn(ColumnName = "comfort_formula", IsNullable = true, Length = 200, ColumnDescription = "舒适度公式", DefaultValue = "", ColumnDataType = "varchar")]
        public string ComfortFormula { get; set; }
        /// <summary>
        /// 月份公式
        ///</summary>
        [DisplayName("月份公式")]
        [SugarColumn(ColumnName = "month_formula", IsNullable = true, Length = 50, ColumnDescription = "月份公式", DefaultValue = "", ColumnDataType = "varchar")]
        public string MonthFormula { get; set; }
        /// <summary>
        /// 单位ID
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "tenant_id", ColumnDescription = "租户ID", DefaultValue = "0", ColumnDataType = "int")]
        public int TenantId { get; set; }
    }
}