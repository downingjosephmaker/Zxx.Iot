using System.ComponentModel;

namespace CenboEventBus
{
    /// <summary>
    /// 插件传递消息事件
    /// </summary>
    public record PluginEvent : IntegrationEvent
    {
        /// <summary>
        /// 插件Guid
        /// </summary>
        [DisplayName("插件Guid")]
        public string PluginGuid { get; set; }

        /// <summary>
        /// 消息传递类
        /// </summary>
        [DisplayName("消息传递类")]
        public PluginMessage Message { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_PluginGuid"></param>
        /// <param name="_Message"></param>
        public PluginEvent(string _PluginGuid, PluginMessage _Message) : base()
        {
            PluginGuid = _PluginGuid;
            Message = _Message;
        }
    }
}
