namespace Service.Services;

/// <summary>
/// Интерфейс для отправки email-уведомлений
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Отправить уведомление об изменении цены
    /// </summary>
    Task SendPriceChangedAsync(string email, string url, decimal oldPrice, decimal newPrice);
    
    /// <summary>
    /// Отправить уведомление о создании подписки
    /// </summary>
    Task SendSubscriptionCreatedAsync(string email, string url, decimal price);
    
    /// <summary>
    /// Отправить уведомление об отписке
    /// </summary>
    Task SendUnsubscribedAsync(string email, string url);
    
    /// <summary>
    /// Отправить список подписок на email
    /// </summary>
    Task SendSubscriptionsListEmailAsync(string email, List<dynamic> subscriptions);
}