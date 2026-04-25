namespace Service.Services;

/// <summary>
/// Интерфейс для получения цены с сайта
/// </summary>
public interface IPriceParser
{
    /// <summary>
    /// Получить текущую цену квартиры по URL
    /// </summary>
    /// <param name="url">Ссылка на объявление</param>
    /// <returns>Цена или null, если не удалось получить</returns>
    Task<int?> GetPriceAsync(string url);
}