using Microsoft.EntityFrameworkCore;
using Service.Database;
using Service.Models;

namespace Service.Services;

/// <summary>
/// Фоновый воркер для проверки и обновления цен подписок
/// </summary>
public class PriceCheckWorker : BackgroundService, IPriceCheckWorker
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IPriceParser priceParser;
    private readonly IEventBus eventBus;
    private readonly ILogger<PriceCheckWorker> logger;
    private readonly TimeSpan checkInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Конструктор воркера
    /// </summary>
    /// <param name="scopeFactory">Фабрика для создания scope"ов</param>
    /// <param name="priceParser">Парсер цен</param>
    /// <param name="eventBus">Шина событий</param>
    /// <param name="logger">Логгер</param>
    public PriceCheckWorker(
        IServiceScopeFactory scopeFactory,
        IPriceParser priceParser,
        IEventBus eventBus,
        ILogger<PriceCheckWorker> logger)
    {
        this.scopeFactory = scopeFactory;
        this.priceParser = priceParser;
        this.eventBus = eventBus;
        this.logger = logger;
    }

    /// <summary>
    /// Основной цикл работы воркера
    /// </summary>
    /// <param name="stoppingToken">Токен отмены</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.logger.LogInformation("Воркер запущен. Интервал проверки: {Interval} минут", this.checkInterval.TotalMinutes);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                this.logger.LogInformation("Начинаем проверку подписок с истекшим TTL...");
                await this.CheckExpiredSubscriptionsAsync();
                this.logger.LogInformation("Проверка подписок завершена");
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Ошибка при проверке подписок");
            }
            
            await Task.Delay(this.checkInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Поиск и проверка подписок с истекшим TTL
    /// </summary>
    private async Task CheckExpiredSubscriptionsAsync()
    {
        using IServiceScope scope = this.scopeFactory.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        List<int> expiredIds = await dbContext.Subscriptions
            .Where(subscription => subscription.IsActive && subscription.Ttl < DateTime.UtcNow)
            .Select(subscription => subscription.Id)
            .ToListAsync();
        
        this.logger.LogInformation("Найдено подписок с истекшим TTL: {Count}", expiredIds.Count);
        
        foreach (int subscriptionId in expiredIds)
        {
            this.logger.LogInformation("Проверяем подписку Id: {Id}", subscriptionId);
            await this.CheckAndUpdateAsync(subscriptionId);
        }
    }

    /// <summary>
    /// Проверка и обновление цены для конкретной подписки
    /// </summary>
    /// <param name="subscriptionId">Идентификатор подписки</param>
    public async Task CheckAndUpdateAsync(int subscriptionId)
    {
        this.logger.LogInformation("Проверка подписки Id: {SubscriptionId}", subscriptionId);
        
        using IServiceScope scope = this.scopeFactory.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        Subscription? subscription = await dbContext.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
        {
            this.logger.LogWarning("Подписка Id: {SubscriptionId} не найдена", subscriptionId);
            return;
        }
        
        if (!subscription.IsActive)
        {
            this.logger.LogInformation("Подписка Id: {SubscriptionId} неактивна, пропускаем", subscriptionId);
            return;
        }
        
        this.logger.LogInformation("Получаем текущую цену для URL: {Url}", subscription.Url);
        
        int? newPrice = await this.priceParser.GetPriceAsync(subscription.Url);
        
        if (!newPrice.HasValue)
        {
            this.logger.LogWarning("Не удалось получить цену для URL: {Url}, деактивируем подписку", subscription.Url);
            
            subscription.GetType().GetProperty("IsUrlValid")?.SetValue(subscription, false);
            subscription.GetType().GetProperty("IsActive")?.SetValue(subscription, false);
            
            await dbContext.SaveChangesAsync();
            return;
        }
        
        int oldPrice = subscription.Price;
        this.logger.LogInformation("Старая цена: {OldPrice}, Новая цена: {NewPrice}", oldPrice, newPrice.Value);
        
        if (oldPrice != newPrice.Value && oldPrice != 0)
        {
            this.logger.LogInformation("Цена изменилась. Обновляем...");
            
            subscription.GetType().GetProperty("Price")?.SetValue(subscription, newPrice.Value);
            subscription.GetType().GetProperty("Ttl")?.SetValue(subscription, DateTime.UtcNow.AddMinutes(5));
            
            await dbContext.SaveChangesAsync();
            
            PriceChangedEvent priceChangedEvent = new PriceChangedEvent
            {
                SubscriptionId = subscription.Id,
                OldPrice = oldPrice,
                NewPrice = newPrice.Value
            };
            
            this.logger.LogInformation("Публикуем событие PriceChangedEvent для подписки Id: {SubscriptionId}", subscription.Id);
            this.eventBus.Publish(priceChangedEvent);
        }
        else if (oldPrice == 0)
        {
            this.logger.LogInformation("Первое обновление цены для подписки Id: {SubscriptionId}", subscription.Id);
            
            subscription.GetType().GetProperty("Price")?.SetValue(subscription, newPrice.Value);
            subscription.GetType().GetProperty("Ttl")?.SetValue(subscription, DateTime.UtcNow.AddMinutes(5));
            
            await dbContext.SaveChangesAsync();
        }
        else
        {
            this.logger.LogInformation("Цена не изменилась, обновляем TTL для подписки Id: {SubscriptionId}", subscription.Id);
            subscription.GetType().GetProperty("Ttl")?.SetValue(subscription, DateTime.UtcNow.AddMinutes(5));
            await dbContext.SaveChangesAsync();
        }
    }
}