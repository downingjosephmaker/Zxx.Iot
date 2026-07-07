using Newtonsoft.Json;
using SqlSugar;
using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 告警屏蔽规则(§9.4:运行时在"告警产生之后、入库通知之前"过滤;
    /// 取代AlarmConfig.DebounceType=3的一刀切屏蔽;
    /// 注意:引擎为后台组件且规则全局加载,故不挂IUnitEntity(与collect_strategy同类决策),
    /// 单位维度屏蔽经MaskScopeType=2+ScopeId表达)
    ///</summary>
    [DisplayName("告警屏蔽规则")]
    [EntityCache]
    [SugarTable(TableName = "alarm_mask", TableDescription = "告警屏蔽规则", IsDisabledUpdateAll = true)]
    public class AlarmMask : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 屏蔽对象类型(1:全局 2:单位 3:建筑 4:设备类型 5:单设备 6:告警等级)
        ///</summary>
        [DisplayName("屏蔽对象类型(1全局2单位3建筑4设备类型5单设备6告警等级)")]
        [SugarColumn(ColumnName = "mask_scope_type", ColumnDescription = "屏蔽对象类型(1全局2单位3建筑4设备类型5单设备6告警等级)", DefaultValue = "1", ColumnDataType = "int")]
        public int MaskScopeType { get; set; } = 1;
        /// <summary>
        /// 屏蔽对象ID(单位/建筑/设备为ID,设备类型为编码,告警等级为等级名;全局为空)
        ///</summary>
        [DisplayName("屏蔽对象ID")]
        [SugarColumn(ColumnName = "scope_id", IsNullable = true, Length = 50, ColumnDescription = "屏蔽对象ID(全局为空)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ScopeId { get; set; }
        /// <summary>
        /// 屏蔽模式(1:永久 2:一次性时间段 3:周期性时间窗)
        ///</summary>
        [DisplayName("屏蔽模式(1永久2一次性3周期窗)")]
        [SugarColumn(ColumnName = "mask_mode", ColumnDescription = "屏蔽模式(1永久2一次性3周期窗)", DefaultValue = "1", ColumnDataType = "int")]
        public int MaskMode { get; set; } = 1;
        /// <summary>
        /// 一次性起始时间(yyyy-MM-dd HH:mm:ss,模式2专用)
        ///</summary>
        [DisplayName("一次性起始时间")]
        [SugarColumn(ColumnName = "start_time", IsNullable = true, Length = 20, ColumnDescription = "一次性起始时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string StartTime { get; set; }
        /// <summary>
        /// 一次性结束时间(yyyy-MM-dd HH:mm:ss,模式2专用)
        ///</summary>
        [DisplayName("一次性结束时间")]
        [SugarColumn(ColumnName = "end_time", IsNullable = true, Length = 20, ColumnDescription = "一次性结束时间", DefaultValue = "", ColumnDataType = "varchar")]
        public string EndTime { get; set; }
        /// <summary>
        /// 周期时间窗JSON(模式3专用,[{"Days":[1,2,3,4,5],"Start":"09:00","End":"18:00"}],Days为星期日=0)
        ///</summary>
        [DisplayName("周期时间窗JSON")]
        [SugarColumn(ColumnName = "time_ranges", IsNullable = true, ColumnDescription = "周期时间窗JSON", ColumnDataType = "text")]
        public string TimeRanges { get; set; }
        /// <summary>
        /// 屏蔽动作(1:完全屏蔽不入库 2:静默,入库打标不通知(默认) 3:降级)
        ///</summary>
        [DisplayName("屏蔽动作(1完全屏蔽2静默3降级)")]
        [SugarColumn(ColumnName = "mask_action", ColumnDescription = "屏蔽动作(1完全屏蔽2静默3降级)", DefaultValue = "2", ColumnDataType = "int")]
        public int MaskAction { get; set; } = 2;
        /// <summary>
        /// 降级目标等级(动作3专用)
        ///</summary>
        [DisplayName("降级目标等级")]
        [SugarColumn(ColumnName = "downgrade_grade", IsNullable = true, Length = 20, ColumnDescription = "降级目标等级", DefaultValue = "", ColumnDataType = "varchar")]
        public string DowngradeGrade { get; set; }
        /// <summary>
        /// 屏蔽原因
        ///</summary>
        [DisplayName("屏蔽原因")]
        [SugarColumn(ColumnName = "reason", IsNullable = true, Length = 200, ColumnDescription = "屏蔽原因", DefaultValue = "", ColumnDataType = "varchar")]
        public string Reason { get; set; }
        /// <summary>
        /// 操作人
        ///</summary>
        [DisplayName("操作人")]
        [SugarColumn(ColumnName = "operator_name", IsNullable = true, Length = 50, ColumnDescription = "操作人", DefaultValue = "", ColumnDataType = "varchar")]
        public string OperatorName { get; set; }
        /// <summary>
        /// 自动失效时间(yyyy-MM-dd HH:mm:ss,到期自动恢复防"忘了解除";空=不失效)
        ///</summary>
        [DisplayName("自动失效时间(空=不失效)")]
        [SugarColumn(ColumnName = "expire_at", IsNullable = true, Length = 20, ColumnDescription = "自动失效时间(空=不失效)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ExpireAt { get; set; }
        /// <summary>
        /// 是否启用(0:否 1:是)
        ///</summary>
        [DisplayName("是否启用(0:否1:是)")]
        [SugarColumn(ColumnName = "is_enable", Length = 1, ColumnDescription = "是否启用(0:否1:是)", DefaultValue = "1", ColumnDataType = "bit")]
        public bool IsEnable { get; set; } = true;
    }

    /// <summary>
    /// 周期时间窗(TimeRanges的JSON元素)
    /// </summary>
    public class AlarmMaskTimeRange
    {
        /// <summary>
        /// 生效星期集合(星期日=0~星期六=6)
        /// </summary>
        public List<int> Days { get; set; } = new();

        /// <summary>
        /// 起始时刻(HH:mm)
        /// </summary>
        public string Start { get; set; } = "";

        /// <summary>
        /// 结束时刻(HH:mm)
        /// </summary>
        public string End { get; set; } = "";
    }
}
