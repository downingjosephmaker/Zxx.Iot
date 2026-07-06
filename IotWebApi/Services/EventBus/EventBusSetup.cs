using CenboEventBus;

namespace IotWebApi;

/// <summary>
/// 事件总线启动器
/// </summary>
public static class EventBusSetup
{
    /// <summary>
    /// 添加事件总线设置
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static void AddEventBusSetup(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // 注册事件总线管理器
        services.AddSingleton<IEventBusManager, EventBusManager>();

        // ================= 自动注册所有事件类型和事件处理器 =================
        var handlerTypes = GetAllEventHandlerTypes();
        foreach (var item in handlerTypes)
        {
            // 注册事件处理器
            services.AddTransient(item.HandlerType);
            // 注册事件总线（如果未注册）
            var eventBusType = typeof(EventBusGeneric<>).MakeGenericType(item.EventType);
            var iEventBusType = typeof(IEventBus<>).MakeGenericType(item.EventType);
            if (!services.Any(s => s.ServiceType == iEventBusType))
            {
                services.AddSingleton(iEventBusType, sp =>
                {
                    var eventBusManager = sp.GetRequiredService<IEventBusManager>();
                    return Activator.CreateInstance(eventBusType, eventBusManager, sp);
                });
            }
        }
        // ================= 自动注册结束 =================

        // ================= 兜底注册：确保关键事件总线可用 =================
        // 自动注册依赖"存在对应的 IIntegrationEventHandler<T>"。
        // PluginService 注入 IEventBus<PluginEvent> 用于向插件发布消息，
        // 正常情况下 PluginEventHandler 会触发自动注册，
        // 此处兜底注册，保证处理器缺失时 PluginService 仍能正常解析、应用可启动。
        EnsureEventBusRegistered<PluginEvent>(services);
    }

    /// <summary>
    /// 确保 <see cref="IEventBus{TEvent}"/> 已注册（若自动注册未覆盖则补注册）。
    /// </summary>
    private static void EnsureEventBusRegistered<TEvent>(IServiceCollection services) where TEvent : IntegrationEvent
    {
        var iEventBusType = typeof(IEventBus<>).MakeGenericType(typeof(TEvent));
        if (services.Any(s => s.ServiceType == iEventBusType)) return;

        var eventBusType = typeof(EventBusGeneric<>).MakeGenericType(typeof(TEvent));
        services.AddSingleton(iEventBusType, sp =>
        {
            var eventBusManager = sp.GetRequiredService<IEventBusManager>();
            return Activator.CreateInstance(eventBusType, eventBusManager, sp);
        });
    }

    /// <summary>
    /// 配置事件总线
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    public static void ConfigureEventBus(this IApplicationBuilder app)
    {
        // ================= 自动订阅所有强类型事件处理器 =================
        var handlerTypes = GetAllEventHandlerTypes();
        foreach (var item in handlerTypes)
        {
            var iEventBusType = typeof(IEventBus<>).MakeGenericType(item.EventType);
            var eventBusInstance = app.ApplicationServices.GetService(iEventBusType);
            if (eventBusInstance != null)
            {
                var subscribeMethod = iEventBusType.GetMethod("Subscribe").MakeGenericMethod(item.HandlerType);
                subscribeMethod.Invoke(eventBusInstance, null);
            }
        }
        // ================= 自动订阅所有强类型事件处理器结束 =================

        // ================= 自动订阅所有动态事件处理器 =================
        var dynamicHandlerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IDynamicIntegrationEventHandler).IsAssignableFrom(t))
            .ToList();

        var registeredDynamicHandlers = new HashSet<string>();
        foreach (var handlerType in dynamicHandlerTypes)
        {
            // 事件类型名约定：去掉Handler后缀
            string eventName = handlerType.Name.EndsWith("Handler") ? handlerType.Name.Substring(0, handlerType.Name.Length - "Handler".Length) : handlerType.Name;
            // 事件类型Type
            var eventType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == eventName && typeof(IntegrationEvent).IsAssignableFrom(t));
            if (eventType == null) continue; // 未找到对应事件类型则跳过
            var eventBusType = typeof(IEventBus<>).MakeGenericType(eventType);
            var eventBusInstance = app.ApplicationServices.GetService(eventBusType);
            if (eventBusInstance != null)
            {
                var subscribeDynamicMethod = eventBusType.GetMethods()
                    .FirstOrDefault(m => m.Name == "SubscribeDynamic" && m.IsGenericMethod);
                if (subscribeDynamicMethod != null)
                {
                    string regKey = handlerType.FullName + "@" + eventName + "@" + eventBusType.FullName;
                    if (registeredDynamicHandlers.Contains(regKey))
                        continue; // 已注册则跳过
                    registeredDynamicHandlers.Add(regKey);
                    var genericMethod = subscribeDynamicMethod.MakeGenericMethod(handlerType);
                    genericMethod.Invoke(eventBusInstance, new object[] { eventName });
                }
            }
        }
        // ================= 自动订阅所有动态事件处理器结束 =================
    }

    /// <summary>
    /// 获取所有实现了IIntegrationEventHandler的泛型类及其事件类型
    /// </summary>
    /// <returns>事件处理器类型和事件类型的集合></returns>
    private static List<(Type HandlerType, Type EventType)> GetAllEventHandlerTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                .Select(i => (HandlerType: t, EventType: i.GetGenericArguments()[0])))
            .ToList();
    }
}
