using System.ComponentModel;

namespace CenboEventBus
{
    /// <summary>
    /// 主程序下发给插件的命令事件
    /// </summary>
    public record PluginCommandEvent : IntegrationEvent
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

        public PluginCommandEvent(string pluginGuid, PluginMessage message) : base()
        {
            PluginGuid = pluginGuid;
            Message = message;
        }
    }
}
