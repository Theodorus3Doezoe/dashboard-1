using System.ComponentModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Platform;
using System.Diagnostics;
using System.Text.Json;
using dashboard.Models; // De namespace van jouw modellen
namespace dashboard
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Property om de voorspelling aan de UI te binden
        private IEnumerable<Hour> _hourlyForecast;
        public IEnumerable<Hour> HourlyForecast
        {
            get => _hourlyForecast;
            set
            {
                _hourlyForecast = value;
                OnPropertyChanged(nameof(HourlyForecast));
            }
        }
        
        private readonly WeatherService _weatherService = new WeatherService();
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

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
                HourlyForecast = forecast.Forecast.ForecastDays
                    .SelectMany(day => day.Hour)
                    .Where(hour => DateTime.Parse(hour.Time) >= startOfCurrentHour)
                    .Take(24);

                // --- EINDE VERNIEUWDE LOGICA ---

                Debug.WriteLine($"--- VOORSPELLING VOOR DE KOMENDE 24 UUR (vanaf {now:HH:mm}) ---");

                foreach (var hourData in HourlyForecast)
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

