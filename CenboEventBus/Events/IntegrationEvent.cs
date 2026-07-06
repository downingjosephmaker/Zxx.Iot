namespace CenboEventBus;

/// <summary>
/// 集成事件基类
/// 所有事件消息的基础类，提供事件的唯一标识和创建时间
/// 用于在微服务或模块之间传递消息
/// </summary>
public record IntegrationEvent
{
    /// <summary>
    /// 默认构造函数
    /// 创建新的集成事件实例，自动生成新的唯一标识和当前时间作为创建时间
    /// </summary>
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreationDate = DateTime.Now;
    }

    /// <summary>
    /// 带参数构造函数
    /// 使用指定的标识和创建时间创建集成事件实例
    /// 常用于事件的重建或持久化场景
    /// </summary>
    /// <param name="id">事件的唯一标识</param>
    /// <param name="createDate">事件的创建时间</param>
    public IntegrationEvent(Guid id, DateTime createDate)
    {
        Id = id;
        CreationDate = createDate;
    }

    /// <summary>
    /// 事件唯一标识
    /// 用于标识和跟踪每个事件实例
    /// </summary>
    public Guid Id { get; private init; }

    /// <summary>
    /// 事件创建时间
    /// 记录事件被创建的确切时间点
    /// </summary>
    public DateTime CreationDate { get; private init; }
}
