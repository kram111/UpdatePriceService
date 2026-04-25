namespace Service.Services;

/// <summary>
/// Базовый интерфейс события
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Идентификатор подписки
    /// </summary>
    int SubscriptionId { get; }
}

/// <summary>
/// Интерфейс шины событий
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Опубликовать событие
    /// </summary>
    void Publish<TEvent>(TEvent eventToPublish) where TEvent : IEvent;
    
    /// <summary>
    /// Подписаться на событие
    /// </summary>
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
}