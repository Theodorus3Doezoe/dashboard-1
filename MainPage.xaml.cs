using System.ComponentModel;
using System.Diagnostics;
using dashboard.Models;
using dashboard.Services;
using System.Globalization;

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

        // Live weergave weerdata
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

        // Services
        private readonly WeatherService _weatherService = new WeatherService();
        private readonly DatabaseHelper _databaseHelper;
        private readonly MqttService _mqttService;

        // Constructor
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


            var html = LoadHtmlFromFile("wwwroot/map.html");
            MapWebView.Source = new HtmlWebViewSource { Html = html };
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

            WeatherApiResponse forecast = await _weatherService.GetWeatherAsync();

            if (forecast != null)
            {
                var now = DateTime.Now;
                var startOfCurrentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

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

        public void ChangeOxygen(string message)
        {
            //MainThread.BeginInvokeOnMainThread(() => OxygenLabel.Text = message);
        }
    }
}