using CenboEventBus;
using IotLog;

namespace IotWebApi
{
    /// <summary>
    /// 通用泛型事件总线，实现IEventBus的泛型类
    /// </summary>
    public class EventBusGeneric<TEvent> : IEventBus<TEvent> where TEvent : IntegrationEvent
    {
        private readonly IEventBusManager _eventBusManager;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// 构造函数-获取依赖注入
        /// </summary>
        /// <param name="eventBusManager"></param>
        /// <param name="serviceProvider"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public EventBusGeneric(IEventBusManager eventBusManager, IServiceProvider serviceProvider)
        {
            _eventBusManager = eventBusManager ?? throw new ArgumentNullException(nameof(eventBusManager));
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 发布事件
        /// </summary>
        /// <param name="event"></param>
        public void Publish(TEvent @event)
        {
            try
            {
                var eventName = typeof(TEvent).Name;
                if (_eventBusManager.HasSubscriptionsForEvent(eventName))
                {
                    var handlers = _eventBusManager.GetHandlersForEvent(eventName);

                    foreach (var handler in handlers)
                    {
                        if (handler.IsDynamic)
                        {
                            // 动态事件处理器
                            var dynamicHandler = _serviceProvider.GetService(handler.HandlerType) as IDynamicIntegrationEventHandler;
                            if (dynamicHandler != null)
                            {
                                Task.Run(async () => await dynamicHandler.Handle(@event));
                            }
                        }
                        else
                        {
                            // 强类型事件处理器
                            var handlerInstance = _serviceProvider.GetService(handler.HandlerType) as IIntegrationEventHandler<TEvent>;
                            if (handlerInstance != null)
                            {
                                Task.Run(async () => await handlerInstance.Handle(@event));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.ErrorLogWrite("EventBusGeneric", "Publish", ex.ToString(), "通用泛型事件总线");
            }
        }

        /// <summary>
        /// 强类型订阅事件
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        public void Subscribe<THandler>() where THandler : IIntegrationEventHandler<TEvent>
        {
            _eventBusManager.AddSubscription<TEvent, THandler>();
        }

        /// <summary>
        ///  取消强类型订阅事件
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        public void Unsubscribe<THandler>() where THandler : IIntegrationEventHandler<TEvent>
        {
            _eventBusManager.RemoveSubscription<TEvent, THandler>();
        }

        /// <summary>
        /// 动态订阅事件
        /// </summary>
        /// <typeparam name="TH">动态事件处理器类型</typeparam>
        /// <param name="eventName">事件名称</param>
        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _eventBusManager.AddDynamicSubscription<TH>(eventName);
        }

        /// <summary>
        /// 取消动态订阅事件
        /// </summary>
        /// <typeparam name="TH">动态事件处理器类型</typeparam>
        /// <param name="eventName">事件名称</param>
        public void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _eventBusManager.RemoveDynamicSubscription<TH>(eventName);
        }
    }
}
