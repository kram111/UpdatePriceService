namespace src.Client;
/// <summary>
/// Класс "приложение", взаимодействие клиента с командной строкой
/// </summary>
public class PriceMonitorApp
{
    private readonly IApiClient apiClient;
    private bool running = true;
    
    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="apiClient"></param>
    public PriceMonitorApp(IApiClient apiClient)
    {
        this.apiClient = apiClient;
    }
    
    /// <summary>
    /// Запуск "приложения"
    /// </summary>
    public async Task Run()
    {
        Console.WriteLine("UpdatePriceService - отслеживание цен на квартиры");
        this.PrintHelp();
        
        while (this.running)
        {
            Console.Write("\n> ");
            string? input = Console.ReadLine();
            
            if (string.IsNullOrEmpty(input))
            {
                continue;
            }
            
            string trimmedInput = input.Trim();
            if (trimmedInput.Length == 0)
            {
                continue;
            }
            
            await this.ParseCommandAsync(trimmedInput);
        }
    }
    
    /// <summary>
    /// Справка
    /// </summary>
    private void PrintHelp()
    {
        Console.WriteLine(@"
Commands:
    sub <email> <url>   - подписка на уведомлении об объекте
    get <email>         - получить все подписки
    del <email> <url>   - удалить подписку на объект
    exit                - выход из программы
");
    }
    
    /// <summary>
    /// Парсер команд
    /// </summary>
    /// <param name="input"></param>
    private async Task ParseCommandAsync(string input)
    {
        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length == 0)
        {
            return;
        }
        
        string command = parts[0].ToLower();
        
        switch (command)
        {
            case "sub":
                if (parts.Length >= 3)
                {
                    bool result = await apiClient.SubscribeAsync(parts[1], parts[2]);
                }
                else
                {
                    Console.WriteLine("Используйте: sub <email> <url>");
                }
                break;
                
            case "get":
                if (parts.Length >= 2)
                {
                    bool result = await apiClient.GetSubscriptionsAsync(parts[1]);
                }
                else
                {
                    Console.WriteLine("Используйте: get <email>");
                }
                break;
                
            case "del":
                if (parts.Length >= 3)
                {
                    bool result = await apiClient.UnsubscribeAsync(parts[1], parts[2]);
                }
                else
                {
                    Console.WriteLine("Использовать: del <email> <url>");
                }
                break;
                
            case "exit":
                Console.WriteLine("Досвидание!");
                this.running = false;
                break;
                
            default:
                Console.WriteLine("Неисзвестная команда");
                break;
        }
    }
}