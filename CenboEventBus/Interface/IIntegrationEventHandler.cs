namespace CenboEventBus;

/// <summary>
/// 集成事件处理器泛型接口
/// 定义处理特定类型集成事件的方法
/// 实现此接口的类负责处理特定类型的事件消息
/// </summary>
/// <typeparam name="TIntegrationEvent">要处理的集成事件类型</typeparam>
public interface IIntegrationEventHandler<in TIntegrationEvent> : IIntegrationEventHandler
    where TIntegrationEvent : IntegrationEvent
{
    /// <summary>
    /// 处理特定类型的集成事件
    /// 实现此方法来定义事件处理的具体逻辑
    /// </summary>
    /// <param name="event">要处理的集成事件实例</param>
    /// <returns>表示异步操作的任务</returns>
    Task Handle(TIntegrationEvent @event);
}

/// <summary>
/// 集成事件处理器基础接口
/// 所有事件处理器接口的基础接口，提供类型标识
/// 用于依赖注入和处理器管理
/// </summary>
public interface IIntegrationEventHandler
{
}
