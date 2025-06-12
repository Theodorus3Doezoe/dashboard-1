using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Platform;
using System.Diagnostics;
using System.Text.Json;
using dashboard.Models; // De namespace van jouw modellen
namespace dashboard
{
    public partial class MainPage : ContentPage
    {
        private readonly WeatherService _weatherService = new WeatherService();
        public MainPage()
        {
            InitializeComponent();

            var html = LoadHtmlFromFile("wwwroot/map.html");
            MapWebView.Source = new HtmlWebViewSource { Html = html };
        }

        string LoadHtmlFromFile(string filename)
        {
            // Get the file from Maui assets
            using var stream = FileSystem.OpenAppPackageFileAsync(filename).Result;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("Pagina verschijnt, bezig met ophalen van weerdata...");

            WeatherApiResponse forecast = await _weatherService.GetWeatherAsync();

            if (forecast != null)
            {
                // --- DEZE HELE LOGICA IS VERNIEUWD ---

                // 1. Rond de huidige tijd af naar het begin van het uur.
                var now = DateTime.Now;
                var startOfCurrentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

                // 2. Bouw de ultieme voorspellingslijst met een LINQ-keten!
                var next24Hours = forecast.Forecast.ForecastDays // Start met de lijst van dagen ([vandaag, morgen])
                    .SelectMany(day => day.Hour) // Pak de 'Hour'-lijsten van ELKE dag en voeg ze samen tot één grote lijst van 48 uur
                    .Where(hour => DateTime.Parse(hour.Time) >= startOfCurrentHour) // Filter hieruit de uren die in de toekomst liggen
                    .Take(24); // Neem van het resultaat de EERSTE 24 items.

                // --- EINDE VERNIEUWDE LOGICA ---

                Debug.WriteLine($"--- VOORSPELLING VOOR DE KOMENDE 24 UUR (vanaf {now:HH:mm}) ---");

                foreach (var hourData in next24Hours)
                {
                    string timeOnly = hourData.Time.Split(' ')[1]; 
                    Debug.WriteLine($"Tijd: {hourData.Time} - Temp: {hourData.TempC}°C - Conditie: {hourData.Condition.Text}");
                }

                Debug.WriteLine("-------------------------------------------------");
            }
            else
            {
                Debug.WriteLine("Fout: Het ophalen van de weerdata is mislukt.");
            }
        }
        }

    }

