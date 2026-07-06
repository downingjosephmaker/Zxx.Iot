using CenboEventBus;
using IotLog;
using IotWebApi.Services;

namespace IotWebApi.Services.EventBus
{
    /// <summary>
    /// 插件上行事件处理器(仅负责把上行消息转投数据入库服务队列)
    /// </summary>
    public class PluginEventHandler : IIntegrationEventHandler<PluginEvent>
    {
        private const string JOB_CATEGORY = "插件上行事件处理器";

        private readonly DataPointIngestService _ingestService;

        public PluginEventHandler(DataPointIngestService ingestService)
        {
            _ingestService = ingestService;
        }

        public Task Handle(PluginEvent @event)
        {
            if (@event.Message == null) return Task.CompletedTask;

            try
            {
                if (!_ingestService.Enqueue(@event))
                {
                    LogHelper.SysLogWrite("PluginEventHandler", "Handle", $"入库队列已关闭，插件[{@event.PluginGuid}]的【{@event.Message.MessageType}】消息未入队。", JOB_CATEGORY);
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("PluginEventHandler", "Handle", ex.ToString(), JOB_CATEGORY);
            }
            return Task.CompletedTask;
        }
    }
}
