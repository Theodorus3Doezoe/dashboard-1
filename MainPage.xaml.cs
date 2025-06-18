using System.Diagnostics;
using dashboard.Models;
using System.ComponentModel;
using System.Globalization;
using dashboard.Services;
using dashboard.Component;
using Location = Microsoft.Maui.Devices.Sensors.Location;

namespace dashboard
{
    // De hoofdlogica achter de dashboardpagina.
    // Deze klasse beheert de kaart, live data via MQTT, GPS-simulatie en gebruikersinteracties.
    public partial class MainPage : ContentPage, INotifyPropertyChanged
    {
        // Wordt aangeroepen wanneer een property-waarde verandert. Essentieel voor data binding.
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly WeatherService _weatherService = new WeatherService();
        private readonly DatabaseHelper _databaseHelper;
        private readonly MqttService _mqttService;

        private IEnumerable<Hour> _hourlyForecast;
        // Bevat de weersvoorspelling voor de komende 24 uur, gebonden aan de UI.
        public IEnumerable<Hour> HourlyForecast
        {
            get => _hourlyForecast;
            set
            {
                _hourlyForecast = value;
                OnPropertyChanged(nameof(HourlyForecast));
            }
        }

        // --- Doelcoördinaten (Objective) ---
        private double? _objectiveLatitude;
        private double? _objectiveLongitude;

        // --- GPS Simulatie ---
        private IDispatcherTimer _simulationTimer;
        private bool _isSimulating = false;
        private double _currentLatitude;
        private double _currentLongitude;
        
        // --- Camera ---
        private bool _isCameraActief;
        private WebView _cameraViewer;

        // --- Constanten ---
        private const double SpeedKmh = 6.0; // Simulatiesnelheid in km/u
        private const double UpdateIntervalSeconds = 1.0; // Hoe vaak de positie wordt bijgewerkt
        private const double MetersPerDegreeLatitude = 111132.954; // Gemiddelde meters per breedtegraad

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            // Initialiseer de WebView voor de camerafeed
            InitializeCameraView();

            // Initialiseer de DatabaseHelper
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string dbPath = Path.Combine(desktopPath, "dashboard.db");
            _databaseHelper = new DatabaseHelper(dbPath);

            // Initialiseer en configureer de MQTT Service
            _mqttService = new MqttService(this, _databaseHelper);
            SubscribeToMqttEvents();

            // Laad de interactieve kaart
            LoadMap();

            // Maak de simulatietimer gereed
            InitializeSimulationTimer();
        }

        // Wordt uitgevoerd wanneer de pagina verschijnt. Start het ophalen van initiële data.
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("Pagina verschijnt, bezig met ophalen van data...");
            
            await _databaseHelper.InitAsync();
            await FetchWeatherForecast();
        }
        
        // Maakt de WebView voor de camera aan en voegt deze toe aan de layout.
        private void InitializeCameraView()
        {
            _cameraViewer = new WebView
            {
                IsVisible = false,
                BackgroundColor = Colors.Black,
            };

            // Voeg de WebView toe aan de Grid, over de kaart heen
            Grid.SetRow(_cameraViewer, 0);
            Grid.SetColumn(_cameraViewer, 0);
            Grid.SetColumnSpan(_cameraViewer, 2);
            ((Grid)Content).Children.Add(_cameraViewer);
        }

        // Abonneert op de events van de MqttService om live data te ontvangen.
        private void SubscribeToMqttEvents()
        {
            _mqttService.TemperatureUpdated += OnTemperatureReceived;
            _mqttService.HeartbeatUpdated += OnHeartbeatReceived;
            _mqttService.ZuurstofUpdated += OnZuurstofReceived;
            _mqttService.DirectionUpdated += OnDirectionReceived;
            _mqttService.GasUpdated += OnGasReceived;
        }

        // Laadt de HTML voor de kaart vanuit een lokaal bestand in de WebView.
        private void LoadMap()
        {
            var html = LoadHtmlFromFile("wwwroot/map.html");
            MapWebView.Source = new HtmlWebViewSource { Html = html };
            MapWebView.Navigating += MapWebView_Navigating;
        }

        // Initialiseert de timer voor de GPS-simulatie.
        private void InitializeSimulationTimer()
        {
            _simulationTimer = Dispatcher.CreateTimer();
            _simulationTimer.Interval = TimeSpan.FromSeconds(UpdateIntervalSeconds);
            _simulationTimer.Tick += OnSimulationTick;
        }

        // Haalt de weersvoorspelling op via de WeatherService en werkt de UI bij.
        private async Task FetchWeatherForecast()
        {
            WeatherApiResponse forecast = await _weatherService.GetWeatherAsync();
            if (forecast != null)
            {
                var now = DateTime.Now;
                var startOfCurrentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
                
                // Filter de voorspelling om te beginnen vanaf het huidige uur voor de komende 24 uur
                HourlyForecast = forecast.Forecast.ForecastDays
                    .SelectMany(day => day.Hour)
                    .Where(hour => DateTime.Parse(hour.Time) >= startOfCurrentHour)
                    .Take(24);
            }
            else
            {
                Debug.WriteLine("Fout: Het ophalen van de weerdata is mislukt.");
            }
        }

        // Verwerkt binnenkomende temperatuurdata van MQTT en werkt de UI bij.
        public void OnTemperatureReceived(string temperature)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (double.TryParse(temperature, NumberStyles.Float, CultureInfo.InvariantCulture, out double tempValue))
                {
                    MyModule.SetTemperature(tempValue.ToString("F1", CultureInfo.InvariantCulture));
                }
            });
        }

        // Verwerkt binnenkomende hartslagdata van MQTT en werkt de UI bij.
        public void OnHeartbeatReceived(string heartbeat)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (double.TryParse(heartbeat, NumberStyles.Integer, CultureInfo.InvariantCulture, out double hbValue))
                {
                    MyModule.SetHeartLabel(hbValue.ToString("F0"));
                }
            });
        }
        
        // Verwerkt binnenkomende zuurstofdata van MQTT en werkt de UI bij.
        public void OnZuurstofReceived(string zuurstof)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (double.TryParse(zuurstof, NumberStyles.Integer, CultureInfo.InvariantCulture, out double o2Value))
                {
                    MyModule.SetZuurstof(o2Value.ToString("F0"));
                }
            });
        }

        // Verwerkt een richting-alert van MQTT en toont deze in de UI.
        public void OnDirectionReceived(string direction)
        {
            if (string.IsNullOrWhiteSpace(direction)) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                string formattedValue = ParseDoubleToString(direction, "F2");
                if (formattedValue == null) return;

                var alert = new DirectionAlert();
                alert.SetDirection(formattedValue);
                AlertsStack.Children.Add(alert);
                _ = RemoveAlertAfterDelay(alert);
            });
        }

        // Verwerkt een gas-alert van MQTT en toont deze in de UI.
        public void OnGasReceived(string gas)
        {
            if (string.IsNullOrWhiteSpace(gas)) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                string formattedValue = ParseDoubleToString(gas, "F2");
                if (formattedValue == null) return;

                var alert = new GasAlert();
                alert.SetGas(formattedValue);
                AlertsStack.Children.Add(alert);
                _ = RemoveAlertAfterDelay(alert);
            });
        }

        // Start de GPS-simulatie.
        private void StartSimulation_Clicked(object sender, EventArgs e)
        {
            if (_isSimulating) return;
            
            _isSimulating = true;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;

            // Startpositie (voorbeeld: Oisterwijk)
            _currentLatitude = 51.539; 
            _currentLongitude = 5.077;
            _simulationTimer.Start();
        }

        // Stopt de GPS-simulatie en verwijdert de marker van de kaart.
        private async void StopSimulation_Clicked(object sender, EventArgs e)
        {
            if (!_isSimulating) return;

            _isSimulating = false;
            _simulationTimer.Stop();
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;

            // Roep JavaScript aan om de GPS marker te verwijderen
            await MapWebView.EvaluateJavaScriptAsync("removeGpsMarker();");
        }
        
        // Schakelt de cameraweergave in of uit.
        private void OnCameraButtonClicked(object sender, EventArgs e)
        {
            _isCameraActief = !_isCameraActief;
            CameraButton.Text = _isCameraActief ? "Stop Camera" : "Camera Meekijken";
    
            if (_isCameraActief)
            {
                // Toon de WebView en laad de videostream
                _cameraViewer.IsVisible = true;
                _cameraViewer.Source = "http://Code:commando@145.93.236.86/api/holographic/stream/live_high.mp4?holo=true&pv=true&mic=true&loopback=true&RenderFromCamera=true";
            }
            else
            {
                // Verberg de WebView en laad een lege pagina om de stream te stoppen
                _cameraViewer.IsVisible = false;
                _cameraViewer.Source = new HtmlWebViewSource { Html = "<html><body style='background-color: black;'></body></html>" };
            }
        }
        
        // Vangt navigatie-events van de WebView op. Wordt gebruikt om data van JavaScript naar C# te sturen.
        private void MapWebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            // Controleer of de URL begint met ons custom protocol "app:"
            if (e.Url != null && e.Url.StartsWith("app:coords"))
            {
                e.Cancel = true; // Annuleer de navigatie, we gebruiken de URL alleen voor data

                try
                {
                    var uri = new Uri(e.Url);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

                    if (double.TryParse(query["lat"], CultureInfo.InvariantCulture, out double lat) &&
                        double.TryParse(query["lng"], CultureInfo.InvariantCulture, out double lng))
                    {
                        _objectiveLatitude = lat;
                        _objectiveLongitude = lng;
                        Debug.WriteLine($"--- NIEUW DOEL INGESTELD: Lat: {_objectiveLatitude}, Lng: {_objectiveLongitude} ---");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fout bij verwerken coördinaten van WebView: {ex.Message}");
                }
            }
        }

        // Wordt elke seconde aangeroepen tijdens de simulatie.
        // Berekent de nieuwe positie en werkt de kaart en data bij.
        private async void OnSimulationTick(object sender, EventArgs e)
        {
            if (!_isSimulating) return;

            // Bereken de verplaatsing
            double speedMetersPerSecond = SpeedKmh * 1000 / 3600;
            double distanceMoved = speedMetersPerSecond * UpdateIntervalSeconds;
            
            // Simuleer een verandering in breedtegraad (simpele beweging naar het noorden)
            double latitudeChange = distanceMoved / MetersPerDegreeLatitude;
            _currentLatitude += latitudeChange;

            // Update de GPS-marker op de kaart via JavaScript
            string script = $"createOrUpdateGpsMarker({_currentLatitude.ToString(CultureInfo.InvariantCulture)}, {_currentLongitude.ToString(CultureInfo.InvariantCulture)});";
            await MapWebView.EvaluateJavaScriptAsync(script);

            // Bereken afstand en richting naar het doel
            var currentLocation = new Location(_currentLatitude, _currentLongitude);
            CalculateDistanceAndBearing(currentLocation);
        }

        // Berekent de afstand en richting van de huidige locatie naar het ingestelde doel.
        // Publiceert het resultaat via MQTT.
        private void CalculateDistanceAndBearing(Location personLocation)
        {
            if (!_objectiveLatitude.HasValue || !_objectiveLongitude.HasValue) return;

            double startLat = personLocation.Latitude;
            double startLng = personLocation.Longitude;
            double endLat = _objectiveLatitude.Value;
            double endLng = _objectiveLongitude.Value;

            double distanceKm = HaversineDistance(startLat, startLng, endLat, endLng);
            double bearingDegrees = CalculateBearing(startLat, startLng, endLat, endLng);

            // Combineer afstand en richting in één string voor MQTT
            string distanceStr = Math.Round(distanceKm, 2).ToString("F2", CultureInfo.InvariantCulture);
            string bearingStr = Math.Round(bearingDegrees, 1).ToString("F1", CultureInfo.InvariantCulture);
            string combinedPayload = $"{distanceStr},{bearingStr}";

            // Publiceer de data
            _mqttService.PublishMessage(MqttService.MqttTopicObjectiveData, combinedPayload);
        }

        // Berekent de afstand tussen twee GPS-coördinaten met de Haversine-formule.
        private double HaversineDistance(double startLat, double startLng, double endLat, double endLng)
        {
            const double EarthRadiusKm = 6371;
            double dLat = (endLat - startLat) * (Math.PI / 180);
            double dLon = (endLng - startLng) * (Math.PI / 180);
            double lat1Rad = startLat * (Math.PI / 180);
            double lat2Rad = endLat * (Math.PI / 180);
            
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + 
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return EarthRadiusKm * c;
        }

        // Berekent de kompasrichting (bearing) van een startpunt naar een eindpunt.
        private double CalculateBearing(double startLat, double startLng, double endLat, double endLng)
        {
            double lat1Rad = startLat * (Math.PI / 180);
            double lon1Rad = startLng * (Math.PI / 180);
            double lat2Rad = endLat * (Math.PI / 180);
            double lon2Rad = endLng * (Math.PI / 180);
            double dLon = lon2Rad - lon1Rad;

            double y = Math.Sin(dLon) * Math.Cos(lat2Rad);
            double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - 
                       Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);
            
            double bearingRad = Math.Atan2(y, x);
            
            // Converteer van radialen naar graden en normaliseer naar 0-360
            return (bearingRad * 180 / Math.PI + 360) % 360;
        }

        // Informeert de UI dat een property is gewijzigd via data binding.
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Leest de inhoud van een bestand uit het app-pakket.
        private string LoadHtmlFromFile(string filename)
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(filename).Result;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        
        // Probeert een string te parsen naar een double en formatteert deze.
        // <returns>De geformatteerde string of null bij een fout.</returns>
        private string ParseDoubleToString(string value, string format)
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedValue))
            {
                return parsedValue.ToString(format, CultureInfo.InvariantCulture);
            }
            return null;
        }
        
        // Verwijdert een UI-element (alert) na een vertraging van 10 seconden.
        private async Task RemoveAlertAfterDelay(View alert)
        {
            await Task.Delay(10000); 
            if (AlertsStack.Children.Contains(alert))
            {
                AlertsStack.Children.Remove(alert);
            }
        }
    }
}
