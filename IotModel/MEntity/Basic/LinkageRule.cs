using Newtonsoft.Json;
using SqlSugar;
using System.Collections.Generic;
using System.ComponentModel;

namespace IotModel
{
    /// <summary>
    /// 规则联动(§10.1:扁平"触发-条件-动作"模型,明确不做BPMN;
    /// 触发:点位变化/告警产生或恢复/定时cron/设备上下线;
    /// 条件:DynamicExpresso表达式(裸参数编码=触发设备点位,d{设备ID}_{参数编码}=跨设备)+时间窗;
    /// 动作:下发命令(白名单)/写虚拟点位/发通知/Webhook;
    /// 引擎为后台组件全局加载,不挂ITenantEntity)
    ///</summary>
    [DisplayName("规则联动")]
    [EntityCache]
    [SugarTable(TableName = "linkage_rule", TableDescription = "规则联动", IsDisabledUpdateAll = true)]
    public class LinkageRule : BaseEntity
    {
        /// <summary>
        /// 雪花主键
        ///</summary>
        [DisplayName("雪花主键")]
        [JsonConverter(typeof(ValueToStringConverter))]
        [SugarColumn(ColumnName = "snow_id", IsPrimaryKey = true, Length = 20, ColumnDescription = "雪花主键", DefaultValue = "0", ColumnDataType = "bigint")]
        public long SnowId { get; set; }
        /// <summary>
        /// 规则名称
        ///</summary>
        [DisplayName("规则名称")]
        [SugarColumn(ColumnName = "rule_name", IsNullable = true, Length = 50, ColumnDescription = "规则名称", DefaultValue = "", ColumnDataType = "varchar")]
        public string RuleName { get; set; }
        /// <summary>
        /// 触发类型(1:点位变化 2:告警产生 3:告警恢复 4:定时cron 5:设备上线 6:设备离线)
        ///</summary>
        [DisplayName("触发类型(1点位2告警3恢复4定时5上线6离线)")]
        [SugarColumn(ColumnName = "trigger_type", ColumnDescription = "触发类型(1点位2告警3恢复4定时5上线6离线)", DefaultValue = "1", ColumnDataType = "int")]
        public int TriggerType { get; set; } = 1;
        /// <summary>
        /// 触发设备ID(0=任意设备)
        ///</summary>
        [DisplayName("触发设备ID(0=任意)")]
        [SugarColumn(ColumnName = "trigger_device_id", ColumnDescription = "触发设备ID(0=任意)", DefaultValue = "0", ColumnDataType = "int")]
        public int TriggerDeviceId { get; set; }
        /// <summary>
        /// 触发参数编码(点位变化型限定参数,空=任意)
        ///</summary>
        [DisplayName("触发参数编码(空=任意)")]
        [SugarColumn(ColumnName = "trigger_param_code", IsNullable = true, Length = 50, ColumnDescription = "触发参数编码(空=任意)", DefaultValue = "", ColumnDataType = "varchar")]
        public string TriggerParamCode { get; set; }
        /// <summary>
        /// 触发cron表达式(定时型专用)
        ///</summary>
        [DisplayName("触发cron表达式")]
        [SugarColumn(ColumnName = "trigger_cron", IsNullable = true, Length = 50, ColumnDescription = "触发cron表达式", DefaultValue = "", ColumnDataType = "varchar")]
        public string TriggerCron { get; set; }
        /// <summary>
        /// 条件表达式(空=恒真;裸参数编码取触发设备点位最新值,d{设备ID}_{参数编码}跨设备取值)
        ///</summary>
        [DisplayName("条件表达式(空=恒真)")]
        [SugarColumn(ColumnName = "condition_formula", IsNullable = true, Length = 300, ColumnDescription = "条件表达式(空=恒真)", DefaultValue = "", ColumnDataType = "varchar")]
        public string ConditionFormula { get; set; }
        /// <summary>
        /// 生效时间窗(JSON,复用AlarmMaskTimeRange结构[{Days:[1-5],Start:"09:00",End:"18:00"}];空=全天)
        ///</summary>
        [DisplayName("生效时间窗(空=全天)")]
        [SugarColumn(ColumnName = "time_ranges", IsNullable = true, ColumnDescription = "生效时间窗(空=全天)", ColumnDataType = "text")]
        public string TimeRanges { get; set; }
        /// <summary>
        /// 动作类型(1:下发命令 2:写虚拟点位 3:发通知 4:Webhook)
        ///</summary>
        [DisplayName("动作类型(1命令2虚拟点位3通知4Webhook)")]
        [SugarColumn(ColumnName = "action_type", ColumnDescription = "动作类型(1命令2虚拟点位3通知4Webhook)", DefaultValue = "3", ColumnDataType = "int")]
        public int ActionType { get; set; } = 3;
        /// <summary>
        /// 动作配置(JSON,按动作类型对应LinkageActionCommand/VirtualPoint/Notify/Webhook)
        ///</summary>
        [DisplayName("动作配置(JSON)")]
        [SugarColumn(ColumnName = "action_config", IsNullable = true, ColumnDescription = "动作配置(JSON)", ColumnDataType = "text")]
        public string ActionConfig { get; set; }
        /// <summary>
        /// 冷却秒数(条件通过后最短再执行间隔,防连发)
        ///</summary>
        [DisplayName("冷却秒数")]
        [SugarColumn(ColumnName = "cooldown_seconds", ColumnDescription = "冷却秒数", DefaultValue = "60", ColumnDataType = "int")]
        public int CooldownSeconds { get; set; } = 60;
        /// <summary>
        /// 是否启用(0:否 1:是)
        ///</summary>
        [DisplayName("是否启用(0:否1:是)")]
        [SugarColumn(ColumnName = "is_enable", Length = 1, ColumnDescription = "是否启用(0:否1:是)", DefaultValue = "1", ColumnDataType = "bit")]
        public bool IsEnable { get; set; } = true;
    }

    /// <summary>
    /// 联动动作:下发设备命令(ClassName须在平台白名单;PluginGuid空=广播全部已加载插件,
    /// 协议插件对不支持的控制类型自行忽略;DeviceIds空=触发设备)
    /// </summary>
    public class LinkageActionCommand
    {
        /// <summary>目标插件Guid(空=广播)</summary>
        public string PluginGuid { get; set; } = "";
        /// <summary>控制类名(netmodbuswrite等)</summary>
        public string ClassName { get; set; } = "";
        /// <summary>控制内容JSON</summary>
        public string ConContent { get; set; } = "";
        /// <summary>目标设备清单(空=触发设备)</summary>
        public List<int> DeviceIds { get; set; } = new List<int>();
    }

    /// <summary>
    /// 联动动作:写虚拟点位(进最新值缓存与遥测管道;DeviceId=0取触发设备)
    /// </summary>
    public class LinkageActionVirtualPoint
    {
        /// <summary>目标设备ID(0=触发设备)</summary>
        public int DeviceId { get; set; }
        /// <summary>虚拟点位参数编码</summary>
        public string ParamCode { get; set; } = "";
        /// <summary>写入值(数值进value,其余进value_str)</summary>
        public string ParamValue { get; set; } = "";
    }

    /// <summary>
    /// 联动动作:发通知(复用notify_channel第一梯队渠道)
    /// </summary>
    public class LinkageActionNotify
    {
        /// <summary>通知内容</summary>
        public string Content { get; set; } = "";
    }

    /// <summary>
    /// 联动动作:调用Webhook(POST JSON)
    /// </summary>
    public class LinkageActionWebhook
    {
        /// <summary>目标地址</summary>
        public string Url { get; set; } = "";
        /// <summary>请求体JSON(空=默认{rule,time})</summary>
        public string Body { get; set; } = "";
    }
}
