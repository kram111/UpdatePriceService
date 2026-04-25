namespace src.Client;
/// <summary>
/// Интерфейс для  http клиента
/// </summary>
public interface IApiClient
{
    /// <summary>
    /// Подписаться на отслеживвание
    /// </summary>
    /// <param name="email">почта</param>
    /// <param name="url">юпл объекта</param>
    /// <returns></returns>
    Task<bool> SubscribeAsync(string email, string url);
    /// <summary>
    /// Получение списка по почте
    /// </summary>
    /// <param name="email">почта</param>
    /// <returns></returns>
    Task<bool> GetSubscriptionsAsync(string email);
    /// <summary>
    /// Удаление подписки
    /// </summary>
    /// <param name="email">почта</param>
    /// <param name="url">юрл</param>
    /// <returns></returns>
    Task<bool> UnsubscribeAsync(string email, string url);
}