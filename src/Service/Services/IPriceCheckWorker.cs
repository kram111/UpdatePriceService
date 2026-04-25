namespace Service.Services;

/// <summary>
/// Интерфейс для проверки цен
/// </summary>
public interface IPriceCheckWorker
{
    /// <summary>
    /// Проверить и обновить цену для конкретной подписки
    /// </summary>
    Task CheckAndUpdateAsync(int subscriptionId);
}