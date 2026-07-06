using SqlSugar;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 防抖模式
    /// </summary>
    public enum DebounceModeEnum
    {
        /// <summary>连续(间隔超窗则重置计数)</summary>
        连续 = 1,
        /// <summary>累计(固定窗口内累计计数)</summary>
        累计 = 2,
    }

    /// <summary>
    /// 防抖动作(仅次数型生效)
    /// </summary>
    public enum DebounceActionEnum
    {
        /// <summary>取第一次：窗口首条立即产生告警，后续屏蔽</summary>
        第一次 = 1,
        /// <summary>取最后一次：窗口内缓冲，到期补发末条</summary>
        最后一次 = 2,
    }

    /// <summary>
    /// 防抖类型(前端据此互斥切换配置项)
    /// </summary>
    public enum DebounceTypeEnum
    {
        /// <summary>次数型：在 X 秒内连续/累计发生 N 次及以上 → 执行动作(第一次/最后一次)。使用 DebounceSeconds/DebounceMode/DebounceCount/DebounceAction</summary>
        次数型 = 1,
        /// <summary>时长型：事件持续满 X 秒且期间未恢复 → 才补发告警。使用 AlarmConfirmSeconds</summary>
        时长型 = 2,
        /// <summary>屏蔽：事件直接丢弃，不告警、不入窗口。无需任何参数</summary>
        屏蔽 = 3,
    }

    /// <summary>
    /// 告警类型管理
    ///</summary>
    [DisplayName("告警类型管理")]
    [EntityCache]
    [SugarTable(TableName = "alarm_config", TableDescription = "告警类型管理", IsDisabledUpdateAll = true)]
    public class AlarmConfig : BaseEntity
    {
        /// <summary>
        /// 告警类型ID
        ///</summary>
        [DisplayName("告警类型ID")]
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "告警类型ID", DefaultValue = "0", ColumnDataType = "int")]
        public int Id { get; set; }
        /// <summary>
        /// 事件类型
        ///</summary>
        [DisplayName("事件类型")]
        [SugarColumn(ColumnName = "event_type", IsNullable = true, Length = 50, ColumnDescription = "事件类型", DefaultValue = "", ColumnDataType = "varchar")]
        public string EventType { get; set; }
        /// <summary>
        /// 报警等级
        ///</summary>
        [DisplayName("报警等级")]
        [SugarColumn(ColumnName = "alarm_grade", IsNullable = true, Length = 20, ColumnDescription = "报警等级", DefaultValue = "", ColumnDataType = "varchar")]
        public string AlarmGrade { get; set; }
        /// <summary>
        /// 报警类型
        ///</summary>
        [DisplayName("报警类型")]
        [SugarColumn(ColumnName = "alarm_type", IsNullable = true, Length = 20, ColumnDescription = "报警类型", DefaultValue = "", ColumnDataType = "varchar")]
        public string AlarmType { get; set; }
        /// <summary>
        /// 参考公式
        ///</summary>
        [DisplayName("参考公式")]
        [SugarColumn(ColumnName = "example_formula", IsNullable = true, Length = 100, ColumnDescription = "参考公式", DefaultValue = "", ColumnDataType = "varchar")]
        public string ExampleFormula { get; set; }
        /// <summary>
        /// 文字模板
        ///</summary>
        [DisplayName("文字模板")]
        [SugarColumn(ColumnName = "text_template", IsNullable = true, Length = 300, ColumnDescription = "文字模板", DefaultValue = "", ColumnDataType = "varchar")]
        public string TextTemplate { get; set; }
        /// <summary>
        /// 是否越限(0:否 1:是)
        ///</summary>
        [DisplayName("是否越限(0:否 1:是)")]
        [SugarColumn(ColumnName = "is_limit", Length = 1, ColumnDescription = "是否越限(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsLimit { get; set; }
        /// <summary>
        /// 是否通知(0:否 1:是)
        ///</summary>
        [DisplayName("是否通知(0:否 1:是)")]
        [SugarColumn(ColumnName = "is_note", Length = 1, ColumnDescription = "是否通知(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsNote { get; set; }

        /// <summary>
        /// 是否开启防抖屏蔽(0:否 1:是)
        ///</summary>
        [DisplayName("是否开启防抖屏蔽(0:否 1:是)")]
        [SugarColumn(ColumnName = "is_debounce", Length = 1, ColumnDescription = "是否开启防抖屏蔽(0:否1:是)", DefaultValue = "0", ColumnDataType = "bit")]
        public bool IsDebounce { get; set; }

        /// <summary>
        /// 防抖类型(1:次数型 2:时长型 3:屏蔽)。前端据此互斥切换：次数型显示"窗口/模式/次数/动作"，时长型显示"确认时长"，屏蔽无参数。
        ///</summary>
        [DisplayName("防抖类型(1:次数型 2:时长型 3:屏蔽)")]
        [EnumRange(typeof(DebounceTypeEnum), "防抖类型值无效")]
        [SugarColumn(ColumnName = "debounce_type", IsNullable = true, ColumnDescription = "防抖类型(1:次数型 2:时长型 3:屏蔽)", DefaultValue = "1", ColumnDataType = "int")]
        public DebounceTypeEnum DebounceType { get; set; } = DebounceTypeEnum.次数型;

        /// <summary>
        /// 防抖时间窗口(秒) —— 次数型专用
        ///</summary>
        [DisplayName("防抖时间窗口(秒)")]
        [SugarColumn(ColumnName = "debounce_seconds", IsNullable = true, ColumnDescription = "防抖时间窗口(秒)", DefaultValue = "60", ColumnDataType = "int")]
        public int DebounceSeconds { get; set; } = 60;

        /// <summary>
        /// 防抖模式(1:连续 2:累计) —— 次数型专用
        ///</summary>
        [DisplayName("防抖模式(1:连续 2:累计)")]
        [EnumRange(typeof(DebounceModeEnum), "防抖模式值无效")]
        [SugarColumn(ColumnName = "debounce_mode", IsNullable = true, ColumnDescription = "防抖模式(1:连续 2:累计)", DefaultValue = "2", ColumnDataType = "int")]
        public DebounceModeEnum DebounceMode { get; set; } = DebounceModeEnum.累计;

        /// <summary>
        /// 防抖次数阈值 —— 次数型专用
        ///</summary>
        [DisplayName("防抖次数阈值")]
        [SugarColumn(ColumnName = "debounce_count", IsNullable = true, ColumnDescription = "防抖次数阈值", DefaultValue = "3", ColumnDataType = "int")]
        public int DebounceCount { get; set; } = 3;

        /// <summary>
        /// 防抖动作(1:第一次 2:最后一次) —— 仅次数型使用
        ///</summary>
        [DisplayName("防抖动作(1:第一次 2:最后一次)")]
        [EnumRange(typeof(DebounceActionEnum), "防抖动作值无效")]
        [SugarColumn(ColumnName = "debounce_action", IsNullable = true, ColumnDescription = "防抖动作(1:第一次 2:最后一次)", DefaultValue = "1", ColumnDataType = "int")]
        public DebounceActionEnum DebounceAction { get; set; } = DebounceActionEnum.第一次;

        /// <summary>
        /// 告警确认时长(秒) —— 时长型专用。持续满该时长且期间未恢复，才由定时任务补发告警。
        /// <para>典型用途：离线告警需"X分钟持续无数据"才确认，避免短暂抖动误报。</para>
        ///</summary>
        [DisplayName("告警确认时长(秒)")]
        [SugarColumn(ColumnName = "alarm_confirm_seconds", IsNullable = true, ColumnDescription = "告警确认时长(秒)", DefaultValue = "0", ColumnDataType = "int")]
        public int AlarmConfirmSeconds { get; set; } = 0;
    }
}