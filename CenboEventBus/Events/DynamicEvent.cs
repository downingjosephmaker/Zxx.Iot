using System.ComponentModel;

namespace CenboEventBus
{
    /// <summary>
    /// 通用动态事件
    /// </summary>
    public record DynamicEvent : IntegrationEvent
    {
        /// <summary>
        /// 数据Json
        /// </summary>
        [DisplayName("数据Json")]
        public string MesJson { get; set; }
        public DynamicEvent(string _MesJson)
        {
            MesJson = _MesJson;
        }
    }
}
