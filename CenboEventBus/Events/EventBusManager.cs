namespace CenboEventBus;

/// <summary>
/// 内存事件总线订阅管理器
/// 用于管理事件总线中的事件订阅，存储事件名称与对应的处理程序之间的映射关系
/// </summary>
public partial class EventBusManager : IEventBusManager
{
    /// <summary>
    /// 存储事件名称和对应处理程序列表的字典
    /// 键: 事件名称 (字符串)
    /// 值: 该事件所有订阅者信息的列表
    /// </summary>
    private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;

    /// <summary>
    /// 存储所有已注册事件的类型列表
    /// 用于类型查找和验证
    /// </summary>
    private readonly List<Type> _eventTypes;

    /// <summary>
    /// 当事件被移除时触发的事件
    /// 可以用于通知其他组件某个事件已经没有订阅者
    /// </summary>
    public event EventHandler<string> OnEventRemoved;

    /// <summary>
    /// 构造函数
    /// 初始化管理器并创建空的处理程序字典和事件类型列表
    /// </summary>
    public EventBusManager()
    {
        _handlers = new Dictionary<string, List<SubscriptionInfo>>();
        _eventTypes = new List<Type>();
    }

    /// <summary>
    /// 检查管理器是否为空
    /// 当没有任何事件订阅时返回true
    /// </summary>
    public bool IsEmpty => !_handlers.Keys.Any();

    /// <summary>
    /// 清空所有订阅
    /// 移除所有已注册的事件订阅关系
    /// </summary>
    public void Clear() => _handlers.Clear();

    /// <summary>
    /// 添加动态事件订阅
    /// 用于订阅不需要强类型绑定的事件，通过字符串事件名称进行订阅
    /// </summary>
    /// <typeparam name="TH">动态事件处理器类型</typeparam>
    /// <param name="eventName">事件名称</param>
    public void AddDynamicSubscription<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        DoAddSubscription(typeof(TH), eventName, isDynamic: true);
    }

    /// <summary>
    /// 添加强类型事件订阅
    /// 将特定类型的事件处理程序与特定类型的事件进行关联
    /// </summary>
    /// <typeparam name="T">事件类型，必须继承自IntegrationEvent</typeparam>
    /// <typeparam name="TH">事件处理器类型，必须实现对应事件的处理接口</typeparam>
    public void AddSubscription<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>();

        DoAddSubscription(typeof(TH), eventName, isDynamic: false);

        if (!_eventTypes.Contains(typeof(T)))
        {
            _eventTypes.Add(typeof(T));
        }
    }

    /// <summary>
    /// 执行添加订阅的内部方法
    /// 将处理程序类型添加到指定事件名称的订阅列表中
    /// </summary>
    /// <param name="handlerType">处理程序类型</param>
    /// <param name="eventName">事件名称</param>
    /// <param name="isDynamic">是否为动态事件处理器</param>
    private void DoAddSubscription(Type handlerType, string eventName, bool isDynamic)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            _handlers.Add(eventName, new List<SubscriptionInfo>());
        }

        if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
        {
            throw new ArgumentException(
                $"处理程序类型 {handlerType.Name} 已经注册到事件 '{eventName}' 中", nameof(handlerType));
        }

        if (isDynamic)
        {
            _handlers[eventName].Add(SubscriptionInfo.Dynamic(handlerType));
        }
        else
        {
            _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
        }
    }

    /// <summary>
    /// 移除动态事件订阅
    /// 取消指定处理程序对指定事件的订阅
    /// </summary>
    /// <typeparam name="TH">动态事件处理器类型</typeparam>
    /// <param name="eventName">事件名称</param>
    public void RemoveDynamicSubscription<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        var handlerToRemove = FindDynamicSubscriptionToRemove<TH>(eventName);
        DoRemoveHandler(eventName, handlerToRemove);
    }

    /// <summary>
    /// 移除强类型事件订阅
    /// 取消指定处理程序对指定类型事件的订阅
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <typeparam name="TH">事件处理器类型</typeparam>
    public void RemoveSubscription<T, TH>()
        where TH : IIntegrationEventHandler<T>
        where T : IntegrationEvent
    {
        var handlerToRemove = FindSubscriptionToRemove<T, TH>();
        var eventName = GetEventKey<T>();
        DoRemoveHandler(eventName, handlerToRemove);
    }

    /// <summary>
    /// 执行移除处理程序的内部方法
    /// 从订阅列表中移除指定的处理程序，并在必要时清理相关资源
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="subsToRemove">要移除的订阅信息</param>
    private void DoRemoveHandler(string eventName, SubscriptionInfo subsToRemove)
    {
        if (subsToRemove != null)
        {
            _handlers[eventName].Remove(subsToRemove);
            if (!_handlers[eventName].Any())
            {
                _handlers.Remove(eventName);
                var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                if (eventType != null)
                {
                    _eventTypes.Remove(eventType);
                }

                RaiseOnEventRemoved(eventName);
            }
        }
    }

    /// <summary>
    /// 获取指定事件类型的所有处理程序
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <returns>该事件类型的所有处理程序信息</returns>
    public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
    {
        var key = GetEventKey<T>();
        return GetHandlersForEvent(key);
    }

    /// <summary>
    /// 根据事件名称获取所有处理程序
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>该事件名称对应的所有处理程序信息</returns>
    public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];

    /// <summary>
    /// 触发事件移除通知
    /// 当某个事件的所有订阅都被移除时通知监听者
    /// </summary>
    /// <param name="eventName">被移除的事件名称</param>
    private void RaiseOnEventRemoved(string eventName)
    {
        var handler = OnEventRemoved;
        handler?.Invoke(this, eventName);
    }

    /// <summary>
    /// 查找要移除的动态订阅信息
    /// </summary>
    /// <typeparam name="TH">动态事件处理器类型</typeparam>
    /// <param name="eventName">事件名称</param>
    /// <returns>要移除的订阅信息，如不存在则返回null</returns>
    private SubscriptionInfo FindDynamicSubscriptionToRemove<TH>(string eventName)
        where TH : IDynamicIntegrationEventHandler
    {
        return DoFindSubscriptionToRemove(eventName, typeof(TH));
    }

    /// <summary>
    /// 查找要移除的强类型订阅信息
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <typeparam name="TH">事件处理器类型</typeparam>
    /// <returns>要移除的订阅信息，如不存在则返回null</returns>
    private SubscriptionInfo FindSubscriptionToRemove<T, TH>()
        where T : IntegrationEvent
        where TH : IIntegrationEventHandler<T>
    {
        var eventName = GetEventKey<T>();
        return DoFindSubscriptionToRemove(eventName, typeof(TH));
    }

    /// <summary>
    /// 执行查找要移除的订阅信息的内部方法
    /// 在指定事件的订阅列表中查找特定类型的处理程序
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <param name="handlerType">处理程序类型</param>
    /// <returns>要移除的订阅信息，如不存在则返回null</returns>
    private SubscriptionInfo DoFindSubscriptionToRemove(string eventName, Type handlerType)
    {
        if (!HasSubscriptionsForEvent(eventName))
        {
            return null;
        }

        return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
    }

    /// <summary>
    /// 检查指定事件类型是否有订阅
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <returns>如果有订阅返回true，否则返回false</returns>
    public bool HasSubscriptionsForEvent<T>() where T : IntegrationEvent
    {
        var key = GetEventKey<T>();
        return HasSubscriptionsForEvent(key);
    }

    /// <summary>
    /// 检查指定事件名称是否有订阅
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>如果有订阅返回true，否则返回false</returns>
    public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);

    /// <summary>
    /// 根据事件名称获取事件类型
    /// </summary>
    /// <param name="eventName">事件名称</param>
    /// <returns>事件类型，如不存在则返回null</returns>
    public Type GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(t => t.Name == eventName);

    /// <summary>
    /// 获取事件类型的唯一标识键
    /// 默认使用类型名称作为事件键
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <returns>事件类型的唯一标识键</returns>
    public string GetEventKey<T>()
    {
        return typeof(T).Name;
    }
}
