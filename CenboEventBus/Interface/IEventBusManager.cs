namespace CenboEventBus;

/// <summary>
/// 事件总线订阅管理器接口
/// 定义管理事件订阅的核心功能，负责存储和维护事件与其处理程序之间的映射关系
/// </summary>
public interface IEventBusManager
{
    /// <summary>
    /// 检查是否存在任何订阅
    /// 当没有任何事件订阅时返回true
    /// </summary>
    bool IsEmpty { get; }
    
    /// <summary>
    /// 事件移除通知事件
    /// 当某个事件的所有订阅都被移除时触发
    /// </summary>
    event EventHandler<string> OnEventRemoved;

    /// <summary>
    /// 添加动态事件订阅
    /// 通过事件名称字符串注册动态事件处理程序
    /// </summary>
    /// <typeparam name="TH">动态事件处理程序类型</typeparam>
    /// <param name="eventName">事件名称</param>
    void AddDynamicSubscription<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;

    /// <summary>
    /// 添加强类型事件订阅
    /// 将特定类型的事件处理程序与特定类型的事件关联起来
    /// </summary>
    /// <typeparam name="T">事件类型，必须继承自IntegrationEvent</typeparam>
    /// <typeparam name="TH">事件处理程序类型，必须实现对应事件的处理接口</typeparam>
    void AddSubscription<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>;

    /// <summary>
    /// 移除强类型事件订阅
    /// 取消特定类型事件处理程序与特定类型事件的关联
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <typeparam name="TH">事件处理程序类型</typeparam>
    void RemoveSubscription<T, TH>()
        where TH : IIntegrationEventHandler<T>
        where T : IntegrationEvent;

    /// <summary>
    /// 移除动态事件订阅
    /// 通过事件名称取消动态事件处理程序的注册
    /// </summary>
    /// <typeparam name="TH">动态事件处理程序类型</typeparam>
    /// <param name="eventName">事件名称</param>
    void RemoveDynamicSubscription<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler;

    /// <summary>
    /// 检查特定类型事件是否有订阅
    /// 判断是否存在针对指定事件类型的处理程序
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <returns>如果有订阅则返回true，否则返回false</returns>
    bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent;

    /// <summary>
    /// 检查特定事件名称是否有订阅
    /// 判断是否存在针对指定事件名称的处理程序
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>如果有订阅则返回true，否则返回false</returns>
    bool HasSubscriptionsForEvent(string eventName);

    /// <summary>
    /// 根据事件名称获取事件类型
    /// 通过事件名称字符串查找对应的事件类型
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>事件类型，如不存在则返回null</returns>
    Type GetEventTypeByName(string eventName);

    /// <summary>
    /// 清空所有订阅
    /// 移除所有已注册的事件和处理程序的关联关系
    /// </summary>
    void Clear();

    /// <summary>
    /// 获取特定类型事件的所有处理程序
    /// 返回针对指定事件类型注册的所有处理程序信息
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <returns>事件处理程序信息的集合</returns>
    IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>()
        where T : IntegrationEvent;

    /// <summary>
    /// 根据事件名称获取所有处理程序
    /// 返回针对指定事件名称注册的所有处理程序信息
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>事件处理程序信息的集合</returns>
    IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName);

    /// <summary>
    /// 获取事件的唯一标识键
    /// 为事件类型生成用于存储和检索的键值
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <returns>事件的唯一标识键</returns>
    string GetEventKey<T>();
}
