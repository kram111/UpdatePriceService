using System.ComponentModel.DataAnnotations;

namespace Service.Models;

/// <summary>
/// Модель подписки (хранится в БД)
/// </summary>
public class Subscription
{
    /// <summary>
    /// Уникальный идентификатор (первичный ключ)
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Email пользователя для уведомлений
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Ссылка на квартиру
    /// </summary>
    [Required(ErrorMessage = "URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Текущая (последняя известная) цена
    /// </summary>
    public int Price { get; private set; }
    
    /// <summary>
    /// Время жизни кэша (когда цена была обновлена в последний раз)
    /// </summary>
    public DateTime Ttl { get; private set; }
    
    /// <summary>
    /// Активна ли подписка (false = пользователь отписался)
    /// </summary>
    public bool IsActive { get; private set; } = true;
    
    /// <summary>
    /// Актуален ли URL (false = объект удалён с сайта)
    /// </summary>
    public bool IsUrlValid { get; private set; } = true;
    
    /// <summary>
    /// Дата создания подписки
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Обновить цену и TTL
    /// </summary>
    public void UpdatePrice(int newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentException("Цена не может быть отрицательной", nameof(newPrice));
        
        Price = newPrice;
        Ttl = DateTime.UtcNow.AddMinutes(5);
    }

    /// <summary>
    /// Обновить только TTL
    /// </summary>
    public void RenewTtl()
    {
        Ttl = DateTime.UtcNow.AddMinutes(5);
    }

    /// <summary>
    /// Активировать подписку
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Деактивировать подписку (отписка)
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Реактивировать подписку с новой ценой
    /// </summary>
    public void Reactivate(int currentPrice)
    {
        IsActive = true;
        IsUrlValid = true;
        UpdatePrice(currentPrice);
    }

    /// <summary>
    /// Отметить URL как недоступный и деактивировать подписку
    /// </summary>
    public void MarkUrlAsInvalid()
    {
        IsUrlValid = false;
        IsActive = false;
    }
}