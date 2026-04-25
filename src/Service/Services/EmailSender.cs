using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Service.Services;

/// <summary>
/// Реализация отправки email через SMTP
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly IConfiguration configuration;
    private readonly ILogger<EmailSender> logger;

    public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    /// <summary>
    /// Отправить уведомление об изменении цены
    /// </summary>
    /// <param name="email">Email получателя</param>
    /// <param name="url">Ссылка на квартиру</param>
    /// <param name="oldPrice">Старая цена</param>
    /// <param name="newPrice">Новая цена</param>
    public async Task SendPriceChangedAsync(string email, string url, decimal oldPrice, decimal newPrice)
    {
        string subject = "Цена на квартиру изменилась";
        string body = $@"
            <h2>Изменение цены</h2>
            <p>Квартира: {url}</p>
            <p>Была цена: <b>{oldPrice:N0} руб.</b></p>
            <p>Стала цена: <b>{newPrice:N0} руб.</b></p>
            <p>Изменение: {newPrice - oldPrice:N0} руб. ({(newPrice - oldPrice) / oldPrice * 100:+.0}%)</p>
            <p style='font-size: 12px; color: #666;'>Автоматическое сообщение от сервиса отслеживания цен.</p>";
        
        await this.SendEmailAsync(email, subject, body);
    }

    /// <summary>
    /// Отправить уведомление о создании подписки
    /// </summary>
    /// <param name="email">Email получателя</param>
    /// <param name="url">Ссылка на квартиру</param>
    /// <param name="price">Текущая цена</param>
    public async Task SendSubscriptionCreatedAsync(string email, string url, decimal price)
    {
        string subject = "Вы подписались на отслеживание цены";
        string body = $@"
            <h2>Подписка оформлена</h2>
            <p>Вы подписались на квартиру: {url}</p>
            <p>Текущая цена: <b>{price:N0} руб.</b></p>
            <p>Мы будем уведомлять вас об изменениях цены.</p>
            <p style='font-size: 12px; color: #666;'>Чтобы отписаться, используйте команду: del {email} {url}</p>";
        
        await this.SendEmailAsync(email, subject, body);
    }

    /// <summary>
    /// Отправить уведомление об отписке
    /// </summary>
    /// <param name="email">Email получателя</param>
    /// <param name="url">Ссылка на квартиру</param>
    public async Task SendUnsubscribedAsync(string email, string url)
    {
        string subject = "Вы отписались от отслеживания цены";
        string body = $@"
            <h2>Отписка оформлена</h2>
            <p>Вы отписались от квартиры: {url}</p>
            <p>Вы больше не будете получать уведомления об изменении цены.</p>
            <p style='font-size: 12px; color: #666;'>Чтобы подписаться используйте команду: sub {email} [ссылка на квартиру]</p>";
        
        await this.SendEmailAsync(email, subject, body);
    }

    /// <summary>
    /// Отправить список всех подписок пользователю на email
    /// </summary>
    /// <param name="email">Email получателя</param>
    /// <param name="subscriptions">Список подписок</param>
    public async Task SendSubscriptionsListEmailAsync(string email, List<dynamic> subscriptions)
    {
        if (subscriptions == null || subscriptions.Count == 0)
        {
            string emptyBody = $@"
                <h2>У вас нет активных подписок</h2>
                <p>Вы можете подписаться на отслеживание цен через команду <b>sub {email} [ссылка на квартиру]</b>.</p>
                <p style='font-size: 12px; color: #666;'>Автоматическое сообщение от сервиса отслеживания цен.</p>";
            
            await this.SendEmailAsync(email, "Ваши подписки на отслеживание цен", emptyBody);
            return;
        }
        
        string subject = $"Ваши подписки на отслеживание цен ({subscriptions.Count})";
        
        StringBuilder itemsHtml = new StringBuilder();
        foreach (dynamic sub in subscriptions)
        {
            string statusText = sub.IsUrlValid ? "Активна" : "Ссылка недоступна";
            itemsHtml.Append($@"
                <div style='border: 1px solid #ddd; border-radius: 8px; padding: 12px; margin-bottom: 12px;'>
                    <p><b>Квартира:</b> {sub.Url}</p>
                    <p><b>Текущая цена:</b> {sub.Price:N0} руб.</p>
                    <p><b>Статус:</b> {statusText}</p>
                    <p style='font-size: 12px; color: #666;'>Чтобы отписаться, используйте команду: del {email} {sub.Url}</p>
                </div>
            ");
        }
        
        string body = $@"
            <h2>Ваш список подписок</h2>
            <p>Всего активных подписок: <b>{subscriptions.Count}</b></p>
            {itemsHtml}
            <p style='font-size: 12px; color: #666;'>Автоматическое сообщение от сервиса отслеживания цен.</p>";
        
        await this.SendEmailAsync(email, subject, body);
    }

    /// <summary>
    /// Отправка email через SMTP
    /// </summary>
    /// <param name="toEmail">Email получателя</param>
    /// <param name="subject">Тема письма</param>
    /// <param name="body">Тело письма (HTML)</param>
    private async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            string? smtpHost = this.configuration["Smtp:Host"];
            string? smtpPortStr = this.configuration["Smtp:Port"];
            string? smtpUser = this.configuration["Smtp:Username"];
            string? smtpPassword = this.configuration["Smtp:Password"];
            string? fromEmail = this.configuration["Smtp:From"];
            
            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                this.logger.LogWarning("SMTP не настроен. Email не отправлен: {ToEmail}", toEmail);
                return;
            }
            
            if (string.IsNullOrEmpty(fromEmail))
            {
                this.logger.LogError("SMTP: Не указан From в настройках");
                return;
            }
            
            int smtpPort = string.IsNullOrEmpty(smtpPortStr) ? 587 : int.Parse(smtpPortStr);
            
            this.logger.LogInformation("SMTP: Host={Host}, Port={Port}, User={User}, From={From}, To={To}", 
                smtpHost, smtpPort, smtpUser, fromEmail, toEmail);
            
            MimeMessage message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            
            BodyBuilder bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();
            
            using SmtpClient client = new SmtpClient();
            
            if (smtpPort == 465)
            {
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
            }
            else
            {
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            }
            
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            this.logger.LogInformation("Email успешно отправлен на {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Ошибка отправки email на {ToEmail}", toEmail);
        }
    }
}