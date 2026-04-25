using Microsoft.EntityFrameworkCore;
using Service.Models;

namespace Service.Database;

/// <summary>
/// Контекст базы данных для Entity Framework Core
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Конструктор, принимающий настройки подключения
    /// </summary>
    /// <param name="options">Настройки контекста</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    /// <summary>
    /// Таблица подписок в базе данных
    /// </summary>
    public DbSet<Subscription> Subscriptions { get; set; }
    
    /// <summary>
    /// Настройка модели базы данных
    /// </summary>
    /// <param name="modelBuilder">Построитель модели</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Subscription>()
            .HasIndex(subscription => new { subscription.Email, subscription.Url })
            .IsUnique()
            .HasDatabaseName("IX_Subscriptions_Email_Url");
        
        modelBuilder.Entity<Subscription>()
            .HasIndex(subscription => subscription.Email)
            .HasDatabaseName("IX_Subscriptions_Email");
        
        modelBuilder.Entity<Subscription>()
            .HasIndex(subscription => subscription.Url)
            .HasDatabaseName("IX_Subscriptions_Url");
        
        modelBuilder.Entity<Subscription>()
            .HasIndex(subscription => new { subscription.IsActive, subscription.Ttl })
            .HasDatabaseName("IX_Subscriptions_IsActive_Ttl");
    }
}