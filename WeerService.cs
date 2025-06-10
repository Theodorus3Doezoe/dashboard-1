using System.Net.Http.Json;
using dashboard.Models; // Belangrijk: verwijst naar de models die we net maakten

namespace dashboard; // Zorg dat de namespace overeenkomt met je projectnaam

public class WeerService
{
    private readonly HttpClient _httpClient = new();

    // ===================================================================
    // HIER is de ENIGE plek waar je je API-sleutel moet invullen.
    // Jouw sleutel moet hier staan, tussen de aanhalingstekens.
    // ===================================================================
    private const string ApiKey = "de7d1c73381d4731a71163324251006";


    public async Task<WeatherData> GetWeatherForLocationAsync(double latitude, double longitude)
    {
        // ===================================================================
        // Deze 'if'-statement moet je NIET aanpassen. Zet hem terug
        // naar de originele controle op de placeholder-tekst.
        // ===================================================================
        if (ApiKey == "VUL_HIER_JE_API_KEY_IN")
        {
            System.Diagnostics.Debug.WriteLine("API Key is niet ingesteld in WeatherService.cs");
            return null;
        }

        var coordinates = $"{latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var url = $"https://api.weatherapi.com/v1/current.json?key={ApiKey}&q={coordinates}&aqi=no";

        try
        {
            return await _httpClient.GetFromJsonAsync<WeatherData>(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fout bij ophalen weerdata: {ex.Message}");
            return null;
        }
    }
}