namespace CenboEventBus;

/// <summary>
/// 事件总线接口
/// 定义事件发布和订阅的核心功能，用于在不同组件间进行松耦合通信
/// </summary>
/// <typeparam name="TEvent">事件类型，必须继承自IntegrationEvent</typeparam>
public interface IEventBus<TEvent> where TEvent : IntegrationEvent
{
    /// <summary>
    /// 发布事件
    /// 将事件消息发送给所有订阅了该事件类型的处理程序
    /// </summary>
    /// <param name="event">要发布的事件实例</param>
    void Publish(TEvent @event);

    /// <summary>
    /// 订阅事件
    /// 注册特定类型事件的处理程序，当该类型事件被发布时，处理程序将被调用
    /// </summary>
    /// <typeparam name="THandler">事件处理程序类型，必须实现对应事件的处理接口</typeparam>
    void Subscribe<THandler>() where THandler : IIntegrationEventHandler<TEvent>;

    /// <summary>
    /// 取消事件订阅
    /// 移除特定类型事件的处理程序注册
    /// </summary>
    /// <typeparam name="THandler">事件处理程序类型</typeparam>
    void Unsubscribe<THandler>() where THandler : IIntegrationEventHandler<TEvent>;

    /// <summary>
    /// 动态订阅事件
    /// 通过事件名称字符串注册动态事件处理程序，用于不需要强类型绑定的场景
    /// </summary>
    /// <typeparam name="TH">动态事件处理程序类型</typeparam>
    /// <param name="eventName">事件名称</param>
    void SubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;

    /// <summary>
    /// 取消动态事件订阅
    /// 通过事件名称字符串移除动态事件处理程序的注册
    /// </summary>
    /// <typeparam name="TH">动态事件处理程序类型</typeparam>
    /// <param name="eventName">事件名称</param>
    void UnsubscribeDynamic<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;
}
