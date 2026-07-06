using CenboEventBus;
using IotLog;
using IotWebApi.Services;

namespace IotWebApi.Services.EventBus
{
    /// <summary>
    /// 策略变更事件处理器(清空策略合并缓存实现热重载,采集调度器/推送引擎下次取用即生效)
    /// </summary>
    public class StrategyChangedEventHandler : IIntegrationEventHandler<StrategyChangedEvent>
    {
        private const string JOB_CATEGORY = "策略变更事件处理器";

        private readonly StrategyMergeService _mergeService;

        public StrategyChangedEventHandler(StrategyMergeService mergeService)
        {
            _mergeService = mergeService;
        }

        public Task Handle(StrategyChangedEvent @event)
        {
            if (@event == null) return Task.CompletedTask;

            try
            {
                _mergeService.Reload();
                LogHelper.SysLogWrite("StrategyChangedEventHandler", "Handle", $"策略变更(种类{@event.StrategyKind}/层级{@event.ScopeType}/对象{@event.ScopeId}),合并缓存已失效待重建。", JOB_CATEGORY);
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("StrategyChangedEventHandler", "Handle", ex.ToString(), JOB_CATEGORY);
            }
            return Task.CompletedTask;
        }
    }
}
