using Newtonsoft.Json;
using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 推送策略表(产品默认→设备覆盖→点位覆盖三级挂靠,管道入口统一执行;
    /// 只约束遥测数据流,告警/上下线事件走独立事件通道不受节流)
    ///</summary>
    [DisplayName("推送策略表")]
    [EntityCache]
    [SugarTable(TableName = "push_strategy", TableDescription = "推送策略表", IsDisabledUpdateAll = true)]
    public class PushStrategy : BaseEntity
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
        /// 推送模式(1=收到即报,2=变化上报,3=定时上报,4=变化上报+最大静默周期兜底,空=未设置)
        ///</summary>
        [DisplayName("推送模式")]
        [SugarColumn(ColumnName = "report_mode", IsNullable = true, ColumnDescription = "推送模式(1=收到即报,2=变化上报,3=定时上报,4=变化+静默兜底)", ColumnDataType = "int")]
        public int? ReportMode { get; set; }
        /// <summary>
        /// 死区类型(0=严格不等,1=绝对死区,2=百分比死区,空=未设置)
        ///</summary>
        [DisplayName("死区类型")]
        [SugarColumn(ColumnName = "deadband_type", IsNullable = true, ColumnDescription = "死区类型(0=严格不等,1=绝对死区,2=百分比死区)", ColumnDataType = "int")]
        public int? DeadbandType { get; set; }
        /// <summary>
        /// 死区值(绝对值或百分比,配合死区类型使用)
        ///</summary>
        [DisplayName("死区值")]
        [SugarColumn(ColumnName = "deadband_value", IsNullable = true, ColumnDescription = "死区值(绝对值或百分比)", ColumnDataType = "decimal(18,4)")]
        public decimal? DeadbandValue { get; set; }
        /// <summary>
        /// 最小推送间隔毫秒(节流窗口内多次变化只推最新一条,空=未设置)
        ///</summary>
        [DisplayName("最小推送间隔毫秒")]
        [SugarColumn(ColumnName = "min_push_interval_ms", IsNullable = true, ColumnDescription = "最小推送间隔毫秒(窗口内只推最新)", ColumnDataType = "int")]
        public int? MinPushIntervalMs { get; set; }
        /// <summary>
        /// 最大静默周期毫秒(值不变超过此时长也强制推一条,空=未设置)
        ///</summary>
        [DisplayName("最大静默周期毫秒")]
        [SugarColumn(ColumnName = "max_silent_ms", IsNullable = true, ColumnDescription = "最大静默周期毫秒(强制上报兜底)", ColumnDataType = "int")]
        public int? MaxSilentMs { get; set; }
        /// <summary>
        /// 关键属性点位清单(|分隔的参数编码,变化立即冲刷不参与合并节流)
        ///</summary>
        [DisplayName("关键属性点位清单")]
        [SugarColumn(ColumnName = "debounce_ignore_keys", IsNullable = true, Length = 500, ColumnDescription = "关键属性点位清单(|分隔,变化立即冲刷)", DefaultValue = "", ColumnDataType = "varchar")]
        public string DebounceIgnoreKeys { get; set; }
    }
}
