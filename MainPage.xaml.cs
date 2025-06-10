using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Platform;

namespace dashboard
{
    public partial class MainPage : ContentPage
    {
        private readonly WeerService _weatherService = new(); // <-- VOEG DEZE REGEL TOE
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

        private async Task LoadWeatherDataAsync()
        {
            // Zet de UI in "laden" staat
            WeatherActivityIndicator.IsRunning = true;
            WeatherLocationLabel.Text = "Locatie ophalen...";

            try
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(30)
                });

                if (location != null)
                {
                    WeatherLocationLabel.Text = "Weer ophalen...";
                    var weatherData = await _weatherService.GetWeatherForLocationAsync(location.Latitude, location.Longitude);

                    if (weatherData != null)
                    {
                        // Vul de UI-elementen met de data
                        WeatherLocationLabel.Text = weatherData.Location.Name;
                        WeatherTemperatureLabel.Text = $"{weatherData.Current.TempC}°C";
                        WeatherConditionLabel.Text = weatherData.Current.Condition.Text;
                        WeatherIconImage.Source = ImageSource.FromUri(new Uri($"https:{weatherData.Current.Condition.Icon}"));
                    }
                    else
                    {
                        WeatherLocationLabel.Text = "Weerdata fout";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Weather Error: {ex.Message}");
                WeatherLocationLabel.Text = "Fout bij ophalen";
                await DisplayAlert("Fout", $"Kon weerdata niet ophalen: {ex.Message}", "OK");
            }
            finally
            {
                // Stop altijd de laad-indicator
                WeatherActivityIndicator.IsRunning = false;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadWeatherDataAsync(); // Roep hier onze nieuwe methode aan
        }
    }
}
