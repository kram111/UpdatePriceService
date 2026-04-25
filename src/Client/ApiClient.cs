using System.Text;
using System.Text.Json;

namespace src.Client;
/// <summary>
/// Клиент для взаимодействия с сервером
/// </summary>
public class ApiClient : IApiClient
{
    private readonly HttpClient httpClient;
    
    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="httpClient"></param>
    public ApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }
    
    /// <summary>
    /// Формируем запрос на подписку
    /// </summary>
    /// <param name="email"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    public async Task<bool> SubscribeAsync(string email, string url)
    {
        try
        {
            Console.WriteLine($"Подписка: {email} -> {url}");
            
            object request = new { url = url, email = email };
            string json = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            
            HttpResponseMessage response = await httpClient.PostAsync("api/subscription/subscribe", content);
            string result = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Успешно {result}");
            }
            else
            {
                Console.WriteLine($"НЕ успешно {result}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ОШИБКА: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Запрос на получаение подписок
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<bool> GetSubscriptionsAsync(string email)
    {
        try
        {
            
            HttpResponseMessage response = await httpClient.GetAsync($"api/subscription/subscriptions?email={Uri.EscapeDataString(email)}");
            string result = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Ваши подписки:");
                Console.WriteLine(JsonFormatter.PrettyPrint(result));
            }
            else
            {
                Console.WriteLine($"НЕ получилось {result}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ОШИБКА: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Формирует запрос на удалегие
    /// </summary>
    /// <param name="email"></param>
    /// <param name="url"></param>
    /// <returns></returns>
    public async Task<bool> UnsubscribeAsync(string email, string url)
    {
        try
        {
            object request = new { url = url, email = email };
            string json = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            
            HttpRequestMessage httpRequest = new HttpRequestMessage();
            httpRequest.Method = HttpMethod.Delete;
            httpRequest.RequestUri = new Uri("api/subscription/unsubscribe", UriKind.Relative);
            httpRequest.Content = content;
            
            HttpResponseMessage response = await httpClient.SendAsync(httpRequest);
            string result = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Успешно {result}");
            }
            else
            {
                Console.WriteLine($"НЕ успешно {result}");
            }
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            return false;
        }
    }
}