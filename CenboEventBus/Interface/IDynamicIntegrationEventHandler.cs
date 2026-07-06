namespace CenboEventBus;

/// <summary>
/// 动态集成事件处理器接口
/// 用于处理不需要强类型绑定的事件，允许通过事件名称字符串进行事件订阅
/// 提供了更灵活的事件处理机制，适用于动态事件处理场景
/// </summary>
public interface IDynamicIntegrationEventHandler
{
    /// <summary>
    /// 处理动态事件
    /// 接收并处理动态类型的事件数据
    /// </summary>
    /// <param name="eventData">动态类型的事件数据，可以是任意类型</param>
    /// <returns>表示异步操作的任务</returns>
    Task Handle(dynamic eventData);
}
