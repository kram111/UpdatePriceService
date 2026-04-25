namespace Service.Services;

/// <summary>
/// Событие изменения цены
/// </summary>
public class PriceChangedEvent : IEvent
{
    /// <summary>
    /// Идентификатор подписки
    /// </summary>
    public int SubscriptionId { get; set; }
    
    /// <summary>
    /// Старая цена
    /// </summary>
    public int OldPrice { get; set; }
    
    /// <summary>
    /// Новая цена
    /// </summary>
    public int NewPrice { get; set; }
}

/// <summary>
/// Реализация шины событий в памяти
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> handlers = new Dictionary<Type, List<Delegate>>();

    /// <summary>
    /// Опубликовать событие всем подписчикам
    /// </summary>
    /// <param name="eventToPublish">Событие для публикации</param>
    /// <typeparam name="TEvent">Тип события</typeparam>
    public void Publish<TEvent>(TEvent eventToPublish) where TEvent : IEvent
    {
        Type eventType = typeof(TEvent);
        
        if (this.handlers.TryGetValue(eventType, out List<Delegate>? handlersList))
        {
            foreach (Delegate handler in handlersList)
            {
                (handler as Action<TEvent>)?.Invoke(eventToPublish);
            }
        }
    }

    /// <summary>
    /// Подписаться на события указанного типа
    /// </summary>
    /// <param name="handler">Обработчик события</param>
    /// <typeparam name="TEvent">Тип события</typeparam>
    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        Type eventType = typeof(TEvent);
        
        if (!this.handlers.ContainsKey(eventType))
        {
            this.handlers[eventType] = new List<Delegate>();
        }
        
        this.handlers[eventType].Add(handler);
    }
}