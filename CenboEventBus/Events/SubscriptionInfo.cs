namespace CenboEventBus;

/// <summary>
/// 订阅信息类
/// 用于存储事件订阅的详细信息，包括订阅类型和处理程序类型
/// 作为事件总线订阅管理的基础数据结构
/// </summary>
public class SubscriptionInfo
{
    /// <summary>
    /// 是否为动态订阅
    /// 动态订阅使用字符串作为事件名称，不需要强类型绑定
    /// </summary>
    public bool IsDynamic { get; }
    
    /// <summary>
    /// 处理程序类型
    /// 存储订阅事件的处理程序的类型信息
    /// </summary>
    public Type HandlerType { get; }

    /// <summary>
    /// 私有构造函数
    /// 通过工厂方法模式创建实例，确保创建过程的一致性
    /// </summary>
    /// <param name="isDynamic">是否为动态订阅</param>
    /// <param name="handlerType">处理程序类型</param>
    private SubscriptionInfo(bool isDynamic, Type handlerType)
    {
        IsDynamic = isDynamic;
        HandlerType = handlerType;
    }

    /// <summary>
    /// 创建动态订阅信息
    /// 用于不需要强类型绑定的事件订阅
    /// </summary>
    /// <param name="handlerType">动态事件处理程序类型</param>
    /// <returns>包含动态订阅信息的实例</returns>
    public static SubscriptionInfo Dynamic(Type handlerType) =>
        new SubscriptionInfo(true, handlerType);

    /// <summary>
    /// 创建类型化订阅信息
    /// 用于需要强类型绑定的事件订阅
    /// </summary>
    /// <param name="handlerType">类型化事件处理程序类型</param>
    /// <returns>包含类型化订阅信息的实例</returns>
    public static SubscriptionInfo Typed(Type handlerType) =>
        new SubscriptionInfo(false, handlerType);
}

