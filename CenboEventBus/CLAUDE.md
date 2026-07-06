[根目录](../CLAUDE.md) > **CenboEventBus**

---

# CenboEventBus - 事件总线框架

## 模块职责

提供轻量级的事件总线实现，支持发布/订阅模式、消息路由、事件持久化等功能。用于模块间解耦通信，如插件系统与主服务之间的数据同步、告警推送等。

---

## 入口与启动

### 模块类型
- **类库项目** (.NET 8.0 Class Library)
- **无独立入口**：被其他项目引用后使用

### 初始化
在主服务启动时注册（`Program.cs`）：
```csharp
// 注册事件总线服务
builder.Services.AddEventBusSetup();

// 配置事件订阅
app.ConfigureEventBus();
```

---

## 对外接口

### 核心接口

#### IEventBus（事件总线）
```csharp
public interface IEventBus
{
    // 发布事件
    Task PublishAsync<TEvent>(TEvent @event) where TEvent : IntegrationEvent;

    // 订阅事件
    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;

    // 取消订阅
    void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;
}
```

#### IEventBusManager（事件管理器）
```csharp
public interface IEventBusManager
{
    // 获取所有订阅信息
    IEnumerable<SubscriptionInfo> GetSubscriptions();

    // 清空订阅
    void Clear();
}
```

#### IIntegrationEventHandler（事件处理器）
```csharp
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task Handle(TEvent @event);
}
```

### 使用示例
```csharp
// 1. 定义事件
public class PluginEvent : IntegrationEvent
{
    public string EventType { get; set; }
    public string DeviceId { get; set; }
    public object Data { get; set; }
    public DateTime Timestamp { get; set; }
}

// 2. 定义事件处理器
public class PluginEventHandler : IIntegrationEventHandler<PluginEvent>
{
    public async Task Handle(PluginEvent @event)
    {
        // 处理事件
        Console.WriteLine($"Received event: {@event.EventType}");
    }
}

// 3. 订阅事件
eventBus.Subscribe<PluginEvent, PluginEventHandler>();

// 4. 发布事件
await eventBus.PublishAsync(new PluginEvent
{
    EventType = "DataChanged",
    DeviceId = "xxx",
    Timestamp = DateTime.Now
});
```

---

## 关键依赖与配置

### NuGet 依赖
- **无**（纯自研框架）

### 项目引用
- 无（独立框架）

---

## 数据模型

### IntegrationEvent（事件基类）
```csharp
public abstract class IntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime CreationDate { get; } = DateTime.Now;
    public string EventType { get; set; }
}
```

### SubscriptionInfo（订阅信息）
```csharp
public class SubscriptionInfo
{
    public string EventName { get; set; }
    public Type HandlerType { get; set; }
    public bool IsDynamic { get; set; }
}
```

### EventBusManager（事件管理器实现）
- 管理所有订阅关系
- 提供事件路由功能
- 支持动态订阅（运行时添加订阅）

---

## 核心功能

### 发布/订阅模式
- **解耦通信**：发布者和订阅者无需直接依赖
- **一对多**：一个事件可被多个处理器订阅
- **异步处理**：所有事件处理都是异步的

### 事件路由
- **按类型路由**：根据事件类型分发到对应的处理器
- **通配符订阅**：支持订阅所有事件（`*`）
- **条件过滤**：支持根据事件内容过滤

### 事件持久化（可选）
- 支持将事件持久化到数据库
- 支持事件重放（用于调试和测试）
- 支持死信队列（处理失败的事件）

---

## 测试与质量

### 当前状态
- **无自动化测试**
- **手动测试**：通过实际业务场景验证

### 建议改进
1. **添加单元测试**：测试发布/订阅逻辑
2. **添加性能测试**：测试高并发事件处理能力
3. **添加监控**：集成 Prometheus 监控事件吞吐量
4. **优化性能**：使用内存队列（如 Channel）提高吞吐量

---

## 常见问题 (FAQ)

### Q1: 如何处理事件处理失败？
- 使用 try-catch 捕获异常
- 记录日志
- 可选：将失败事件放入死信队列，供后续处理

### Q2: 如何确保事件顺序？
- 当前实现不保证事件顺序（并发处理）
- 如需保证顺序，可在事件处理器中加锁
- 或使用顺序消息队列（如 RabbitMQ、Kafka）

### Q3: 如何跨进程通信？
- 当前实现仅支持进程内通信
- 如需跨进程，可集成消息队列（RabbitMQ、Kafka、Redis Stream）
- 或使用 gRPC、SignalR 等

### Q4: 如何优化性能？
- 使用 `System.Threading.Channels` 作为内存队列
- 批量处理事件（减少 IO 操作）
- 使用对象池减少 GC 压力
- 异步处理所有事件

---

## 相关文件清单

### 目录结构
```
CenboEventBus/
├── Events/                   # 事件定义
│   ├── IntegrationEvent.cs   # 事件基类
│   ├── SubscriptionInfo.cs   # 订阅信息
│   └── EventBusManager.cs    # 事件管理器
├── Interface/                # 接口定义
│   ├── IEventBus.cs          # 事件总线接口
│   ├── IEventBusManager.cs   # 管理器接口
│   └── IIntegrationEventHandler.cs # 事件处理器接口
├── obj/                      # 编译输出
└── CenboEventBus.csproj      # 项目文件
```

---

## 变更记录 (Changelog)

### 2026-04-24
- 初始化模块文档
- 完成事件总线架构梳理

---

**最后更新**：2026-04-24 09:44:55
