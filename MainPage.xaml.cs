using System.ComponentModel;
using Microsoft.Maui.Devices.Sensors; // Belangrijk voor Location
using System.Diagnostics;
using dashboard.Models;
using Microsoft.Maui.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Maui.Dispatching;
using System.Threading;
using Microsoft.Maui.Storage; // Voor FileSystem
using System.IO;
using dashboard.Services;

namespace dashboard
{
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        // --- EVENTHANDLER VOOR PROPERTYCHANGED ---
        public event PropertyChangedEventHandler PropertyChanged;

        // --- SERVICES ---
        private readonly WeatherService _weatherService = new WeatherService();
        private readonly DatabaseHelper _databaseHelper;
        private readonly MqttService _mqttService;

        // --- PROPERTIES VOOR DATA BINDING ---
        private IEnumerable<Hour> _hourlyForecast;
        public IEnumerable<Hour> HourlyForecast
        {
            get => _hourlyForecast;
            set { _hourlyForecast = value; OnPropertyChanged(nameof(HourlyForecast)); }
        }

        // --- VARIABELEN VOOR DOEL (OBJECTIVE) ---
        private double? objectiveLatitude;
        private double? objectiveLongitude;

        // --- VARIABELEN VOOR GPS SIMULATIE ---
        private IDispatcherTimer simulationTimer;
        private bool isSimulating = false;
        private double currentLatitude;
        private double currentLongitude;

        // --- CONSTANTEN ---
        private const double SpeedKmh = 6.0;
        private const double UpdateIntervalSeconds = 1.0;
        private const double MetersPerDegreeLatitude = 111132.954;

        // --- CONSTRUCTOR ---
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            // Initialiseer DatabaseHelper met pad op de desktop
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string dbPath = Path.Combine(desktopPath, "dashboard.db");
            _databaseHelper = new DatabaseHelper(dbPath);

            // Initialiseer MQTT Service
            _mqttService = new MqttService(this, _databaseHelper);
            _mqttService.TemperatureUpdated += OnTemperatureReceived;
            _mqttService.HeartbeatUpdated += OnHeartbeatReceived;
            _mqttService.ZuurstofUpdated += OnZuurstofReceived;

            // Laad de kaart in de WebView
            var html = LoadHtmlFromFile("wwwroot/map.html");
            MapWebView.Source = new HtmlWebViewSource { Html = html };
            MapWebView.Navigating += MapWebView_Navigating;

            // Initialiseer de simulatie timer
            InitializeSimulationTimer();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- METHODES BIJ OPSTARTEN PAGINA ---
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("Pagina verschijnt, bezig met ophalen van data...");
            await _databaseHelper.InitAsync();

            // Haal weersvoorspelling op
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
            else
            {
                Debug.WriteLine("Fout: Het ophalen van de weerdata is mislukt.");
            }

            // Start een timer voor het loggen van live MQTT data in de terminal
            Device.StartTimer(TimeSpan.FromSeconds(2), () =>
            {
                Debug.WriteLine("--------- LIVE MQTT DATA ---------");
                Debug.WriteLine($"Temperatuur: {_mqttService.LatestTemperature}");
                Debug.WriteLine($"Geluid: {_mqttService.LatestSound}");
                // Voeg hier eventueel andere waarden toe die je wilt loggen
                return true; // Timer blijft herhalen
            });
        }

        // --- METHODES VOOR LIVE DATA (MQTT) ---
        public void OnTemperatureReceived(string temperature)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (double.TryParse(temperature, NumberStyles.Float, CultureInfo.InvariantCulture, out double tempValue))
                {
                    string roundedTemp = tempValue.ToString("F1", CultureInfo.InvariantCulture);
                    // Aanname: MyModule is een UI-element in je XAML, b.v. <local:MyControl x:Name="MyModule" />
                    MyModule.SetTemperature(roundedTemp);
                }
                else
                {
                    MyModule.SetTemperature(temperature); // Fallback
                }
            });
        }

        public void OnHeartbeatReceived(string heartbeat)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (double.TryParse(heartbeat, NumberStyles.Integer, CultureInfo.InvariantCulture, out double hbValue))
                {
                    string roundedHb = hbValue.ToString("F0"); // Hartslag meestal zonder decimalen
                    MyModule.SetHeartLabel(roundedHb);
                }
                else
                {
                    MyModule.SetHeartLabel(heartbeat); // Fallback
                }
            });
        }
        
        public void OnZuurstofReceived(string zuurstof) // Parameter naam gecorrigeerd voor duidelijkheid
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (double.TryParse(zuurstof, NumberStyles.Integer, CultureInfo.InvariantCulture, out double o2Value))
                {
                    string roundedO2 = o2Value.ToString("F0"); // Zuurstofsaturatie meestal zonder decimalen
                    MyModule.SetZuurstof(roundedO2);
                }
                else
                {
                    MyModule.SetZuurstof("N/A"); // Fallback
                }
            });
        }

        // --- METHODES VOOR KAART & SIMULATIE ---
        string LoadHtmlFromFile(string filename)
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(filename).Result;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        
        private void InitializeSimulationTimer()
        {
            simulationTimer = Dispatcher.CreateTimer();
            simulationTimer.Interval = TimeSpan.FromSeconds(UpdateIntervalSeconds);
            simulationTimer.Tick += OnSimulationTick;
        }

        private void StartSimulation_Clicked(object sender, EventArgs e)
        {
            if (isSimulating) return;
            isSimulating = true;
            // StartButton.IsEnabled = false; // Schakel knoppen uit/in indien nodig
            // StopButton.IsEnabled = true;

            currentLatitude = 51.539; // Startpositie (voorbeeld)
            currentLongitude = 5.077;
            simulationTimer.Start();
        }

        private async void StopSimulation_Clicked(object sender, EventArgs e)
        {
            if (!isSimulating) return;
            isSimulating = false;
            simulationTimer.Stop();
            // StartButton.IsEnabled = true;
            // StopButton.IsEnabled = false;

            await MapWebView.EvaluateJavaScriptAsync("removeGpsMarker();");
        }

        private async void OnSimulationTick(object sender, EventArgs e)
        {
            if (!isSimulating) return;

            // Bereken nieuwe positie
            double speedMetersPerSecond = SpeedKmh * 1000 / 3600;
            double distanceMoved = speedMetersPerSecond * UpdateIntervalSeconds;
            double latitudeChange = distanceMoved / MetersPerDegreeLatitude;
            currentLatitude += latitudeChange;

            // Update GPS marker op de kaart
            string script = $"createOrUpdateGpsMarker({currentLatitude.ToString(CultureInfo.InvariantCulture)}, {currentLongitude.ToString(CultureInfo.InvariantCulture)});";
            await MapWebView.EvaluateJavaScriptAsync(script);

            // Bereken afstand en richting naar doel
            var currentLocation = new Microsoft.Maui.Devices.Sensors.Location(currentLatitude, currentLongitude);
            CalculateDistanceAndBearing(currentLocation);
        }

        private void MapWebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            // Vang de coördinaten op die vanuit JavaScript worden gestuurd
            if (e.Url.StartsWith("app:coords"))
            {
                e.Cancel = true; // Voorkom daadwerkelijke navigatie
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
        }
        
        // --- BEREKENINGEN VOOR AFSTAND EN RICHTING ---
        private void CalculateDistanceAndBearing(Microsoft.Maui.Devices.Sensors.Location personLocation)
        {
            if (!objectiveLatitude.HasValue || !objectiveLongitude.HasValue) return;

            double startLat = personLocation.Latitude;
            double startLng = personLocation.Longitude;
            double endLat = objectiveLatitude.Value;
            double endLng = objectiveLongitude.Value;

            double distanceKm = CalculateDistance(startLat, startLng, endLat, endLng);
            double bearingDegrees = CalculateBearing(startLat, startLng, endLat, endLng);
            
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

        // --- OUDE UI UPDATE METHODES (UITGECOMMENTEERD) ---
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
        //     MainThread.BeginInvokeOnMainThread(() => OxygenLabel.Text = message);
        // }
    }
}