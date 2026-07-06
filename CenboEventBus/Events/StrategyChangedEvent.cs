using System.ComponentModel;

namespace CenboEventBus
{
    /// <summary>
    /// 策略变更事件(配置页写库后发布,采集调度器/推送策略引擎热重载运行时参数,无需重启插件)
    /// </summary>
    public record StrategyChangedEvent : IntegrationEvent
    {
        /// <summary>
        /// 策略种类(1=采集策略,2=推送策略)
        /// </summary>
        [DisplayName("策略种类(1=采集策略,2=推送策略)")]
        public int StrategyKind { get; set; }

        /// <summary>
        /// 挂靠层级(1=产品,2=设备,3=点位)
        /// </summary>
        [DisplayName("挂靠层级(1=产品,2=设备,3=点位)")]
        public int ScopeType { get; set; }

        /// <summary>
        /// 挂靠对象(产品=设备类型编码,设备/点位=设备ID)
        /// </summary>
        [DisplayName("挂靠对象(产品=设备类型编码,设备/点位=设备ID)")]
        public string ScopeId { get; set; }

        /// <summary>
        /// 参数编码(仅点位级)
        /// </summary>
        [DisplayName("参数编码(仅点位级)")]
        public string ParamCode { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_StrategyKind"></param>
        /// <param name="_ScopeType"></param>
        /// <param name="_ScopeId"></param>
        /// <param name="_ParamCode"></param>
        public StrategyChangedEvent(int _StrategyKind, int _ScopeType, string _ScopeId, string _ParamCode = "") : base()
        {
            StrategyKind = _StrategyKind;
            ScopeType = _ScopeType;
            ScopeId = _ScopeId;
            ParamCode = _ParamCode;
        }
    }
}
