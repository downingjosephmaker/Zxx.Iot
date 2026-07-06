using CenboEventBus;
using IotLog;

namespace IotWebApi.Services.EventBus
{
    /// <summary>
    /// 插件命令事件处理器
    /// </summary>
    public class PluginCommandEventHandler : IIntegrationEventHandler<PluginCommandEvent>
    {
        private const string JOB_CATEGORY = "插件命令事件处理器";

        public async Task Handle(PluginCommandEvent @event)
        {
            if (@event.Message == null) return;

            try
            {
                if (OperatorCommon.DicPlugins.TryGetValue(@event.PluginGuid, out var plugin))
                {
                    //LogHelper.SysLogWrite("PluginCommandEventHandler", "Handle", $"({@event.Message.CurrentTime})【{plugin.PluginGuid}】{plugin.PluginName}【{@event.Message.MessageType}】下发：{@event.Message.MessageJson}", JOB_CATEGORY);
                    await plugin.ReceiveMessageAsync(@event.Message);
                }
                else
                {
                    LogHelper.SysLogWrite("PluginCommandEventHandler", "Handle", $"插件[{@event.PluginGuid}]未加载，命令未下发。", JOB_CATEGORY);
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("PluginCommandEventHandler", "Handle", ex.ToString(), JOB_CATEGORY);
            }
        }
    }
}
