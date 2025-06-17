using System.ComponentModel;
using System.Diagnostics;
using dashboard.Models;
using dashboard.Services;
using System.Globalization;
using Microsoft.Maui.Devices.Sensors; // Belangrijk voor Location
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Maui.Dispatching;
using System.Threading; // Toegevoegd voor CancellationToken, hoewel niet direct gebruikt in de simulatie, is het goede praktijk.


namespace dashboard
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {

        // PropertyChanged voor binding
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Services
        private readonly DatabaseHelper _databaseHelper;
        private readonly MqttService _mqttService;

        // Constructor

        // Variabelen voor Objective
        private double? objectiveLatitude;
        private double? objectiveLongitude;

        // Variabelen voor de GPS simulatie
        private IDispatcherTimer simulationTimer;
        private bool isSimulating = false;
        private double currentLatitude;
        private double currentLongitude;

        // Constanten voor de berekening
        private const double SpeedKmh = 6.0;
        private const double UpdateIntervalSeconds = 1.0;
        private const double MetersPerDegreeLatitude = 111132.954;

        private IEnumerable<Hour> _hourlyForecast;
        public IEnumerable<Hour> HourlyForecast
        {
            get => _hourlyForecast;
            set { _hourlyForecast = value; OnPropertyChanged(nameof(HourlyForecast)); }
        }

        private readonly WeatherService _weatherService = new WeatherService();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string dbPath = Path.Combine(desktopPath, "dashboard.db");
            _databaseHelper = new DatabaseHelper(dbPath);
            _mqttService = new MqttService(this, _databaseHelper);

            // Hier koppel je de event handler
            _mqttService.TemperatureUpdated += OnTemperatureReceived;
            _mqttService.HeartbeatUpdated += OnHeartbeatReceived;
            _mqttService.ZuurstofUpdated += OnZuurstofReceived;

            var html = LoadHtmlFromFile("wwwroot/map.html");
            MapWebView.Source = new HtmlWebViewSource { Html = html };
            MapWebView.Navigating += MapWebView_Navigating;

            InitializeSimulationTimer();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        // Laad lokaal html bestand in webview
        string LoadHtmlFromFile(string filename)
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(filename).Result;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // Initialiseer bij pagina openen
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("Pagina verschijnt, bezig met ophalen van weerdata...");
            await _databaseHelper.InitAsync();
        private void InitializeSimulationTimer()
        {
            simulationTimer = Dispatcher.CreateTimer();
            simulationTimer.Interval = TimeSpan.FromSeconds(UpdateIntervalSeconds);
            simulationTimer.Tick += OnSimulationTick;
        }

        // Event handler voor de "Start" knop
        private void StartSimulation_Clicked(object sender, EventArgs e)
        {
            if (isSimulating) return;
            isSimulating = true;
            // Aanname dat de knoppen StartButton en StopButton heten in je XAML
            // StartButton.IsEnabled = false;
            // StopButton.IsEnabled = true;

            currentLatitude = 51.539; // Startpositie
            currentLongitude = 5.077;
            simulationTimer.Start();
        }

        // Event handler voor de "Stop" knop
        private async void StopSimulation_Clicked(object sender, EventArgs e)
        {
            if (!isSimulating) return;
            isSimulating = false;
            simulationTimer.Stop();
            // StartButton.IsEnabled = true;
            // StopButton.IsEnabled = false;

            await MapWebView.EvaluateJavaScriptAsync("removeGpsMarker();");
        }

        // Dit is waar de magie gebeurt, elke seconde
        private async void OnSimulationTick(object sender, EventArgs e)
        {
            if (!isSimulating) return;

            // Bereken de nieuwe positie
            double speedMetersPerSecond = SpeedKmh * 1000 / 3600;
            double distanceMoved = speedMetersPerSecond * UpdateIntervalSeconds;
            double latitudeChange = distanceMoved / MetersPerDegreeLatitude;
            currentLatitude += latitudeChange;

            // Roep JavaScript aan om de marker op de kaart te updaten
            string script = $"createOrUpdateGpsMarker({currentLatitude.ToString(CultureInfo.InvariantCulture)}, {currentLongitude.ToString(CultureInfo.InvariantCulture)});";
            await MapWebView.EvaluateJavaScriptAsync(script);

            // ✨ AANGEPAST: Roep de berekening aan met de nieuwe positie
            var currentLocation = new Microsoft.Maui.Devices.Sensors.Location(currentLatitude, currentLongitude);
            CalculateDistanceAndBearing(currentLocation);
        }

        protected override async void OnAppearing()
        {
            WeatherApiResponse forecast = await _weatherService.GetWeatherAsync();
            if (forecast != null)
            {
                var now = DateTime.Now;
                var startOfCurrentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
                HourlyForecast = forecast.Forecast.ForecastDays
                    .SelectMany(day => day.Hour)
                    .Where(hour => DateTime.Parse(hour.Time) >= startOfCurrentHour)
                    .Take(24);
            }
        }


                HourlyForecast = forecast.Forecast.ForecastDays
                    .SelectMany(day => day.Hour)
                    .Where(hour => DateTime.Parse(hour.Time) >= startOfCurrentHour)
                    .Take(24);

                foreach (var hourData in HourlyForecast)
                {
                    Debug.WriteLine(
                        $"Tijd: {hourData.Time} - Temp: {hourData.TempC}°C - Conditie: {hourData.Condition.Text}");
                }
            }
            else
            {
                Debug.WriteLine("Fout: Het ophalen van de weerdata is mislukt.");

        private void MapWebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            if (e.Url.StartsWith("app:coords"))
            {
                e.Cancel = true;
                var uri = new Uri(e.Url);
                var query = uri.Query.TrimStart('?')
                    .Split('&')
                    .Select(part => part.Split('='))
                    .ToDictionary(kv => kv[0], kv => kv[1]);

                if (query.TryGetValue("lat", out var latStr) && query.TryGetValue("lng", out var lngStr))
                {
                    objectiveLatitude = double.Parse(latStr, CultureInfo.InvariantCulture);
                    objectiveLongitude = double.Parse(lngStr, CultureInfo.InvariantCulture);
                    Debug.WriteLine($"--- NIEUW DOEL INGESTELD: Lat: {objectiveLatitude}, Lng: {objectiveLongitude} ---");
                }

            }

            // MQTT LIVE DATA LOGGEN IN TERMINAL
            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
            {
                Debug.WriteLine("--------- LIVE MQTT DATA ---------");
                Debug.WriteLine($"Temperatuur: {_mqttService.LatestTemperature}");
                Debug.WriteLine($"Geluid: {_mqttService.LatestSound}");
                // Debug.WriteLine($"Gas: {_mqttService.LatestGas}");
                //Debug.WriteLine($"Locatie: {_mqttService.LatestLocation}");
                // Debug.WriteLine($"Oxygen: {_mqttService.LatestOxygen}");
                // Debug.WriteLine($"Hartslag: {_mqttService.LatestHeartbeat}");
                return true; // blijf herhalen
            });
        }

// LIVE WEERGAVE (MQTT)
        public void OnTemperatureReceived(string temperature)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (double.TryParse(temperature, NumberStyles.Float, CultureInfo.InvariantCulture,
                        out double tempValue))
                {
                    string roundedTemp = tempValue.ToString("F1", CultureInfo.InvariantCulture);
                    MyModule.SetTemperature(roundedTemp);
                }
                else
                {
                    // fallback, toon originele string als het niet lukt te converteren
                    MyModule.SetTemperature(temperature);
                }
            });
        }

        public void OnHeartbeatReceived(string heartbeat)
        {
            Debug.WriteLine(heartbeat);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (double.TryParse(heartbeat, NumberStyles.Integer, CultureInfo.InvariantCulture, out double hbValue))
                {
                    string roundedHb = hbValue.ToString("F1", CultureInfo.InvariantCulture);
                    MyModule.SetHeartLabel(roundedHb);
                }
                else
                {
                    MyModule.SetHeartLabel(heartbeat);

                }
            });
        }
        
        public void OnZuurstofReceived(string heartbeat)
        {
            Debug.WriteLine(heartbeat);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (double.TryParse(heartbeat, NumberStyles.Integer, CultureInfo.InvariantCulture, out double hbValue))
                {
                    string roundedHb = hbValue.ToString("F1", CultureInfo.InvariantCulture);
                    MyModule.SetZuurstof(roundedHb);
                }
                else
                {
                    MyModule.SetZuurstof("N/A");

                }
            });
        }






        // public void ShowDirection(string message)
        // {
        //     MainThread.BeginInvokeOnMainThread(() => DirectionLabel.Text = message);
        // }

        // public void ShowSound(string message)
        // {
        //     MainThread.BeginInvokeOnMainThread(() => SoundLabel.Text = message);
        // }

        // public void ChangeHeartbeat(string message)
        // {
        //     MainThread.BeginInvokeOnMainThread(() => HeartLabel.Text = message);
        // }

       // public void ChangeOxygen(string message)
       // {
            //MainThread.BeginInvokeOnMainThread(() => OxygenLabel.Text = message);
        }
    }

        // ✨ GECORRIGEERD: Gebruik de volledige namespace om de fout op te lossen
        private void CalculateDistanceAndBearing(Microsoft.Maui.Devices.Sensors.Location personLocation)
        {
            if (!objectiveLatitude.HasValue || !objectiveLongitude.HasValue) return;

            double startLat = personLocation.Latitude;
            double startLng = personLocation.Longitude;
            double endLat = objectiveLatitude.Value;
            double endLng = objectiveLongitude.Value;

            double distanceKm = CalculateDistance(startLat, startLng, endLat, endLng);
            double bearingDegrees = CalculateBearing(startLat, startLng, endLat, endLng);
            // string cardinalDirection = BearingToCardinal(bearingDegrees);

            Debug.WriteLine($"Afstand: {distanceKm:F2} km | Richting: {bearingDegrees:F1}°");
        }

        private double CalculateDistance(double startLat, double startLng, double endLat, double endLng)
        {
            const double EarthRadiusKm = 6371;
            double dLat = (endLat - startLat) * (Math.PI / 180);
            double dLon = (endLng - startLng) * (Math.PI / 180);
            double lat1Rad = startLat * (Math.PI / 180);
            double lat2Rad = endLat * (Math.PI / 180);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        private double CalculateBearing(double startLat, double startLng, double endLat, double endLng)
        {
            double lat1Rad = startLat * (Math.PI / 180);
            double lon1Rad = startLng * (Math.PI / 180);
            double lat2Rad = endLat * (Math.PI / 180);
            double lon2Rad = endLng * (Math.PI / 180);
            double dLon = lon2Rad - lon1Rad;
            double y = Math.Sin(dLon) * Math.Cos(lat2Rad);
            double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);
            double bearingRad = Math.Atan2(y, x);
            return (bearingRad * 180 / Math.PI + 360) % 360;
        }
    }
}
