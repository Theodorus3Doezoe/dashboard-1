using System.Text.Json.Serialization;

namespace dashboard.Models; // Zorg dat de namespace overeenkomt met je projectnaam

public class WeatherLocation
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; }
}

public class WeatherCondition
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }
}

public class CurrentWeather
{
    [JsonPropertyName("temp_c")]
    public double TempC { get; set; }

    [JsonPropertyName("condition")]
    public WeatherCondition Condition { get; set; }
}

public class WeatherData
{
    [JsonPropertyName("location")]
    public WeatherLocation Location { get; set; }

    [JsonPropertyName("current")]
    public CurrentWeather Current { get; set; }
}