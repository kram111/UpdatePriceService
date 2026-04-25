using src.Client;

namespace Client;

class Program
{
    static async Task Main(string[] args)
    {
        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:5054/");
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        ApiClient apiClient = new ApiClient(httpClient);
        PriceMonitorApp app = new PriceMonitorApp(apiClient);
        
        await app.Run();
    }
}