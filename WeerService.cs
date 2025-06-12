using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using dashboard.Models;

namespace dashboard;
public class WeatherService
{
    
    private readonly HttpClient _httpClient = new HttpClient();
    private const string ApiKey = "de7d1c73381d4731a71163324251006";
    private const string City = "Tilburg";

    public async Task<WeatherApiResponse> GetWeatherAsync()
    {
        string url = $"https://api.weatherapi.com/v1/forecast.json?key={ApiKey}&q={City}&days=2&aqi=no&alerts=no";

        try
        {
            // Verstuur de request en krijg de JSON-string terug
            string jsonResponse = await _httpClient.GetStringAsync(url);

            // Converteer de JSON-string naar een C# object (zie WeerModels.cs hieronder)
            WeatherApiResponse forecast = JsonSerializer.Deserialize<WeatherApiResponse>(jsonResponse);
            Console.WriteLine(forecast);
            return forecast;
        }
        catch (HttpRequestException e)
        {
            // Log de fout of geef null terug
            Console.WriteLine($"Fout bij het ophalen van weerdata: {e.Message}");
            return null;
        }
    }
}