using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Service.Database;
using Service.Models;
using Service.Services;

namespace Service;
/// <summary>
/// Класс,точка входа в сервис
/// </summary>
public class Program
{
    /// <summary>
    /// Точка входа
    /// </summary>
    /// <param name="args"></param>
    public static void Main(string[] args)
    { 
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IServiceCollection services = builder.Services; 
        
        services.AddControllers();
        services.AddEndpointsApiExplorer(); 
        services.AddSwaggerGen();
        
        services.AddDbContext<ApplicationDbContext>(
            (DbContextOptionsBuilder options) => 
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
        );
        
        services.AddHttpClient<IPriceParser, PriceParser>();
        
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IPriceCheckWorker, PriceCheckWorker>();
        services.AddSingleton<IEventBus, InMemoryEventBus>();
        services.AddHostedService<PriceCheckWorker>();
        
        WebApplication app = builder.Build();
        
        using (IServiceScope scope = app.Services.CreateScope())
        {
            ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.EnsureCreated();
        }
        
        using (IServiceScope scope = app.Services.CreateScope())
        {
            IEventBus eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            IServiceProvider serviceProvider = scope.ServiceProvider;

            eventBus.Subscribe<PriceChangedEvent>(async (PriceChangedEvent priceChangedEvent) =>
            { 
                using IServiceScope handlerScope = serviceProvider.CreateScope();
                ApplicationDbContext dbContext = handlerScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                IEmailSender emailSender = handlerScope.ServiceProvider.GetRequiredService<IEmailSender>();
                ILogger<Program> logger = handlerScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                Subscription? subscription = await dbContext.Subscriptions.FindAsync(priceChangedEvent.SubscriptionId);
                if (subscription != null)
                {
                    await emailSender.SendPriceChangedAsync(
                        subscription.Email,
                        subscription.Url,
                        priceChangedEvent.OldPrice,
                        priceChangedEvent.NewPrice
                    );
                    logger.LogInformation("Отправлено уведомление об изменении цены для подписки Id: {SubscriptionId}", priceChangedEvent.SubscriptionId);
                }
                else
                {
                    logger.LogWarning("Подписка Id: {SubscriptionId} не найдена при обработке события", priceChangedEvent.SubscriptionId);
                }
            });
        }
        
        if (app.Environment.IsDevelopment())
        { 
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}