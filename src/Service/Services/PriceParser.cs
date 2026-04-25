using System.Text.RegularExpressions;

namespace Service.Services;

/// <summary>
/// Реализация парсера через HTML
/// </summary>
public class PriceParser : IPriceParser
{
    private readonly HttpClient httpClient;
    private readonly ILogger<PriceParser> logger;

    public PriceParser(HttpClient httpClient, ILogger<PriceParser> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;

        this.httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    /// <summary>
    /// Получить текущую цену квартиры по URL
    /// </summary>
    /// <param name="url">Ссылка на объявление</param>
    /// <returns>Цена или null, если не удалось получить</returns>
    public async Task<int?> GetPriceAsync(string url)
    {
        this.logger.LogInformation("Начинаем парсинг цены для URL: {Url}", url);
        
        try
        {
            int? result = await this.ParseFromHtmlAsync(url);
            
            if (result.HasValue)
            {
                this.logger.LogInformation("Парсинг успешен: цена = {Price} для {Url}", result.Value, url);
            }
            else
            {
                this.logger.LogWarning("Не удалось получить цену для {Url}", url);
            }
            
            return result;
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Ошибка парсинга цены для {Url}", url);
            return null;
        }
    }

    /// <summary>
    /// Парсинг цены из HTML страницы
    /// </summary>
    /// <param name="url">Ссылка на объявление</param>
    /// <returns>Цена или null</returns>
    private async Task<int?> ParseFromHtmlAsync(string url)
    {
        this.logger.LogInformation("Загружаем HTML страницы: {Url}", url);
    
        string html = await this.httpClient.GetStringAsync(url);
    
        this.logger.LogInformation("HTML загружен, размер: {Length} символов", html.Length);

        this.logger.LogInformation("Ищем цену по классу с захватом до символа рубля...");
        
        string pattern = @"FullPaymentMethodsItem_promo_price[^>]*>.*?(\d[\d\s&;nbsp;]*\d).*?₽";
        Match match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        if (match.Success)
        {
            string priceText = match.Groups[1].Value;
            this.logger.LogInformation("Найдена цена по шаблону с рублем: '{PriceText}'", priceText);
            int? price = this.ParsePriceText(priceText);
            if (price.HasValue)
            {
                return price;
            }
        }

        this.logger.LogWarning("Не удалось найти цену на странице {Url}", url);
        return null;
    }

    /// <summary>
    /// Очистка текста цены от лишних символов и преобразование в число
    /// </summary>
    /// <param name="priceText">Текст с ценой</param>
    /// <returns>Цена или null</returns>
    private int? ParsePriceText(string priceText)
    {
        this.logger.LogInformation("Очищаем текст цены: '{PriceText}'", priceText);
        
        try
        {
            if (string.IsNullOrWhiteSpace(priceText))
            {
                this.logger.LogWarning("Пустой текст цены");
                return null;
            }

            string cleanPrice = priceText
                .Replace("&nbsp;", "")
                .Replace("&thinsp;", "")
                .Replace(" ", "")
                .Replace("\u00A0", "")
                .Trim();
            
            cleanPrice = Regex.Replace(cleanPrice, @"[^\d]", "");
            
            this.logger.LogInformation("После очистки: '{CleanPrice}'", cleanPrice);

            if (string.IsNullOrEmpty(cleanPrice))
            {
                this.logger.LogWarning("Не удалось извлечь цифры из текста: {PriceText}", priceText);
                return null;
            }

            int price = int.Parse(cleanPrice);
            this.logger.LogInformation("Цена успешно распарсена: {Original} -> {Price}", priceText, price);
            return price;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Ошибка парсинга цены из текста: {PriceText}", priceText);
            return null;
        }
    }
}