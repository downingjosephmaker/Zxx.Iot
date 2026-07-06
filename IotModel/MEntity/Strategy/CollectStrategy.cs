using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 采集策略表(产品默认→设备覆盖→点位覆盖三级挂靠,运行时按点位>设备>产品逐字段合并)
    ///</summary>
    [DisplayName("采集策略表")]
    [EntityCache]
    [SugarTable(TableName = "collect_strategy", TableDescription = "采集策略表", IsDisabledUpdateAll = true)]
    public class CollectStrategy : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 单位ID(暂不挂IUnitEntity:合并引擎为后台组件且合并结果全局共享,待超管旁路机制落地后纳入隔离)
        ///</summary>
        [DisplayName("单位ID")]
        [SugarColumn(ColumnName = "unit_id", ColumnDescription = "单位ID", DefaultValue = "0", ColumnDataType = "int")]
        public int UnitId { get; set; }
        /// <summary>
        /// 挂靠层级(1=产品,2=设备,3=点位)
        ///</summary>
        [DisplayName("挂靠层级(1=产品,2=设备,3=点位)")]
        [SugarColumn(ColumnName = "scope_type", ColumnDescription = "挂靠层级(1=产品,2=设备,3=点位)", DefaultValue = "1", ColumnDataType = "int")]
        public int ScopeType { get; set; } = 1;
        /// <summary>
        /// 挂靠对象(产品=设备类型编码,设备/点位=设备ID)
        ///</summary>
        [DisplayName("挂靠对象(产品=设备类型编码,设备/点位=设备ID)")]
        [SugarColumn(ColumnName = "scope_id", Length = 50, ColumnDescription = "挂靠对象(产品=设备类型编码,设备/点位=设备ID)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ScopeId { get; set; }
        /// <summary>
        /// 参数编码(仅点位级使用)
        ///</summary>
        [DisplayName("参数编码(仅点位级使用)")]
        [SugarColumn(ColumnName = "param_code", IsNullable = true, Length = 100, ColumnDescription = "参数编码(仅点位级使用)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ParamCode { get; set; }
        /// <summary>
        /// 采集周期毫秒(从物理设备采集的周期,空=未设置回落下级)
        ///</summary>
        [DisplayName("采集周期毫秒")]
        [SugarColumn(ColumnName = "collect_cycle_ms", IsNullable = true, ColumnDescription = "采集周期毫秒(空=未设置)", ColumnDataType = "int")]
        public int? CollectCycleMs { get; set; }
        /// <summary>
        /// 采集cron表达式(低频场景毫秒/cron双模,设置后优先于采集周期)
        ///</summary>
        [DisplayName("采集cron表达式")]
        [SugarColumn(ColumnName = "collect_cron", IsNullable = true, Length = 50, ColumnDescription = "采集cron表达式(低频场景,设置后优先于采集周期)", DefaultValue = "", ColumnDataType = "varchar")]
        public string CollectCron { get; set; }
        /// <summary>
        /// 上报最大周期毫秒(与采集解耦:采集可快、上报可慢,空=未设置)
        ///</summary>
        [DisplayName("上报最大周期毫秒")]
        [SugarColumn(ColumnName = "report_cycle_ms", IsNullable = true, ColumnDescription = "上报最大周期毫秒(空=未设置)", ColumnDataType = "int")]
        public int? ReportCycleMs { get; set; }
    }
}
