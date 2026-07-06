using CenboEventBus;

namespace IotWebApi.Services.EventBus
{
    /// <summary>
    /// 动态插件消息事件处理器（主程序自动订阅，处理所有DynamicEventHandler事件）
    /// </summary>
    public class DynamicEventHandler : IDynamicIntegrationEventHandler
    {
        public Task Handle(dynamic eventData)
        {
            // 这里可以根据eventData内容做自定义处理
            string pluginName = eventData?.PluginName ?? "未知插件";
            string message = eventData?.Message ?? "";
            DateTime time = eventData?.Time ?? DateTime.Now;
            Console.WriteLine($"[动态事件] 收到插件消息：{pluginName} - {message} - {time}");
            // 可扩展：写入日志、数据库、推送等
            return Task.CompletedTask;
        }
    }
}