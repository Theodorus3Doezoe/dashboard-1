// Bestand: WeerModels.cs

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace dashboard.Models // Zorg dat de namespace overeenkomt met je projectstructuur
{
    // Dit is de 'hoofdklasse' die de volledige JSON-response van de API omvat.
    // In je WeatherService moet je nu dit type gebruiken: Task<WeatherApiResponse>
    public class WeatherApiResponse
    {
        [JsonPropertyName("location")]
        public Location Location { get; set; }

        [JsonPropertyName("current")]
        public CurrentWeather Current { get; set; }

        [JsonPropertyName("forecast")]
        public Forecast Forecast { get; set; }
    }

    public class Location
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }

    public class CurrentWeather
    {
        [JsonPropertyName("temp_c")]
        public double TempC { get; set; }

        [JsonPropertyName("condition")]
        public Condition Condition { get; set; }
    }

    public class Condition
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("icon")]
        public string IconUrl { get; set; }
    }

    public class Forecast
    {
        // De API geeft een array terug, dus we gebruiken een List<>
        [JsonPropertyName("forecastday")]
        public List<ForecastDay> ForecastDays { get; set; }
    }

    public class ForecastDay
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        // 'day' is een object binnen 'forecastday' met de samenvatting van het weer
        [JsonPropertyName("day")]
        public DaySummary Day { get; set; }
        
        [JsonPropertyName("hour")]
        public List<Hour> Hour { get; set; }
    }

    public class DaySummary
    {
        [JsonPropertyName("maxtemp_c")]
        public double MaxTempC { get; set; }

        [JsonPropertyName("mintemp_c")]
        public double MinTempC { get; set; }

        [JsonPropertyName("avgtemp_c")]
        public double AvgTempC { get; set; }

        [JsonPropertyName("condition")]
        public Condition Condition { get; set; }
    }
    
    public class Hour
    {
        [JsonPropertyName("time")]
        public string Time { get; set; }

        [JsonPropertyName("temp_c")]
        public double TempC { get; set; }
        
        // We kunnen de bestaande Condition klasse hier hergebruiken!
        [JsonPropertyName("condition")]
        public Condition Condition { get; set; }

        [JsonPropertyName("wind_kph")]
        public double WindKph { get; set; }
    }
    
    
}
