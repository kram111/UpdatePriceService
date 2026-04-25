using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Service.Database;
using Service.Models;
using Service.Services;

namespace Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    private readonly ApplicationDbContext dbContext;
    private readonly IPriceParser priceParser;
    private readonly IEmailSender emailSender;
    private readonly IPriceCheckWorker worker;
    private readonly ILogger<SubscriptionController> logger;

    public SubscriptionController(
        ApplicationDbContext dbContext,
        IPriceParser priceParser,
        IEmailSender emailSender,
        IPriceCheckWorker worker,
        ILogger<SubscriptionController> logger)
    {
        this.dbContext = dbContext;
        this.priceParser = priceParser;
        this.emailSender = emailSender;
        this.worker = worker;
        this.logger = logger;
    }

    /// <summary>
    /// Подписка на отслеживание цены
    /// </summary>
    /// <param name="request">Запрос с email и url</param>
    /// <returns>Результат подписки</returns>
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        this.logger.LogInformation("POST /api/subscribe получен. Url: {Url}, Email: {Email}", request.Url, request.Email);
        
        try
        {
            this.logger.LogInformation("Проверка существующей подписки для Email: {Email}, Url: {Url}", request.Email, request.Url);
            
            Subscription? existingSubscription = await this.dbContext.Subscriptions
                .FirstOrDefaultAsync(subscription => 
                    subscription.Url == request.Url && 
                    subscription.Email == request.Email);
            
            if (existingSubscription != null && existingSubscription.IsActive)
            {
                this.logger.LogWarning("Пользователь {Email} уже подписан на URL: {Url}", request.Email, request.Url);
                return this.BadRequest(new { error = "Вы уже подписаны на этот URL" });
            }
            
            this.logger.LogInformation("Получение текущей цены для URL: {Url}", request.Url);
            
            int? currentPrice = await this.priceParser.GetPriceAsync(request.Url);
            if (!currentPrice.HasValue)
            {
                this.logger.LogWarning("Не удалось получить цену для URL: {Url}", request.Url);
                return this.BadRequest(new { error = "Не удалось получить цену по этому URL" });
            }
            
            this.logger.LogInformation("Цена получена: {Price} для URL: {Url}", currentPrice.Value, request.Url);
            
            Subscription subscription;
            
            if (existingSubscription != null && !existingSubscription.IsActive)
            {
                this.logger.LogInformation("Реактивация существующей подписки для пользователя: {Email}, Url: {Url}", request.Email, request.Url);
                
                existingSubscription.Reactivate(currentPrice.Value);
                subscription = existingSubscription;
            }
            else
            {
                this.logger.LogInformation("Создание новой подписки для пользователя: {Email}, Url: {Url}", request.Email, request.Url);
                
                subscription = new Subscription
                {
                    Email = request.Email,
                    Url = request.Url,
                    CreatedAt = DateTime.UtcNow
                };
                subscription.Reactivate(currentPrice.Value);
                this.dbContext.Subscriptions.Add(subscription);
            }
            
            await this.dbContext.SaveChangesAsync();
            this.logger.LogInformation("Подписка сохранена в базе данных. Id: {Id}", subscription.Id);
            
            this.logger.LogInformation("Отправка письма-подтверждения на: {Email}", request.Email);
            await this.emailSender.SendSubscriptionCreatedAsync(request.Email, request.Url, currentPrice.Value);
            
            this.logger.LogInformation("Запуск начальной проверки цены для подписки Id: {Id}", subscription.Id);
            await this.worker.CheckAndUpdateAsync(subscription.Id);
            
            this.logger.LogInformation("Подписка успешно создана. Id: {Id}", subscription.Id);
            
            return this.Ok(new 
            { 
                message = "Подписка успешно создана", 
                id = subscription.Id,
                url = subscription.Url,
                email = subscription.Email,
                currentPrice = currentPrice.Value
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            this.logger.LogWarning(ex, "Попытка дублирования подписки для Email: {Email}, Url: {Url}", request.Email, request.Url);
            return this.BadRequest(new { error = "Вы уже подписаны на этот URL" });
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Ошибка создания подписки для {Url}", request.Url);
            return this.StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Получение списка подписок пользователя
    /// </summary>
    /// <param name="email">Email пользователя</param>
    /// <returns>Список подписок</returns>
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions([FromQuery] string email)
    {
        this.logger.LogInformation("GET /api/subscription/subscriptions получен. Email: {Email}", email);
    
        try
        {
            var subscriptions = await this.dbContext.Subscriptions
                .Where(subscription => subscription.Email == email && subscription.IsActive)
                .Select(subscription => new 
                { 
                    subscription.Url, 
                    subscription.Price, 
                    subscription.Ttl, 
                    subscription.IsUrlValid 
                })
                .ToListAsync();
        
            _ = Task.Run(async () =>
            {
                try
                {
                    var dynamicList = subscriptions.Cast<dynamic>().ToList();
                    await this.emailSender.SendSubscriptionsListEmailAsync(email, dynamicList);
                    this.logger.LogInformation("Список подписок отправлен на {Email}", email);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Не удалось отправить список подписок на {Email}", email);
                }
            });
        
            this.logger.LogInformation("Получено {Count} подписок для email: {Email}", subscriptions.Count, email);
            return this.Ok(subscriptions);
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Ошибка получения подписок для {Email}", email);
            return this.StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }

    /// <summary>
    /// Отписка от отслеживания цены
    /// </summary>
    /// <param name="request">Запрос с email и url</param>
    /// <returns>Результат отписки</returns>
    [HttpDelete("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] UnsubscribeRequest request)
    {
        this.logger.LogInformation("DELETE /api/unsubscribe получен. Url: {Url}, Email: {Email}", request.Url, request.Email);
        
        try
        {
            Subscription? subscription = await this.dbContext.Subscriptions
                .FirstOrDefaultAsync(subscription => 
                    subscription.Url == request.Url && 
                    subscription.Email == request.Email && 
                    subscription.IsActive);
            
            if (subscription == null)
            {
                this.logger.LogWarning("Подписка не найдена для Url: {Url}, Email: {Email}", request.Url, request.Email);
                return this.NotFound(new { error = "Подписка не найдена" });
            }
            
            subscription.Deactivate();
            await this.dbContext.SaveChangesAsync();
            
            this.logger.LogInformation("Подписка деактивирована. Id: {Id}", subscription.Id);
            
            await this.emailSender.SendUnsubscribedAsync(request.Email, request.Url);
            
            return this.Ok(new { message = "Отписка выполнена успешно" });
        }
        catch (Exception exception)
        {
            this.logger.LogError(exception, "Ошибка отписки {Url} для {Email}", request.Url, request.Email);
            return this.StatusCode(500, new { error = "Внутренняя ошибка сервера" });
        }
    }
}

/// <summary>
/// Запрос на подписку
/// </summary>
public class SubscribeRequest
{
    /// <summary>
    /// Ссылка на квартиру
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Email пользователя
    /// </summary>
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Запрос на отписку
/// </summary>
public class UnsubscribeRequest
{
    /// <summary>
    /// Ссылка на квартиру
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Email пользователя
    /// </summary>
    public string Email { get; set; } = string.Empty;
}