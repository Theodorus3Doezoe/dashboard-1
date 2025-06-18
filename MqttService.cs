using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using dashboard.Services;
using dashboard.Tables;
using System.Diagnostics;
using System.Globalization;

namespace dashboard
{
    public class MqttService
    {
        private readonly MainPage main;
        private readonly DatabaseHelper _databaseHelper;
        private MqttClient _mqttClient;

        private const string BrokerAddress = "3b2688bc320d4ceaaa3f7aa16f7bfb63.s1.eu.hivemq.cloud";
        private const int BrokerPort = 8883;
        private const string MqttUser = "CodeCommandos";
        private const string MqttPassword = "CodeCommandos1";

        private const string MqttTopicZuurstof = "SPO2_Data";
        private const string MqttTopicHartslag = "heartbeat";
        private const string MqttTopicGasDetection = "MQ2 sensor";
        private const string MqttTopicSound = "geluid sensor";
        private const string MqttTopicTemperature = "temperature sensor";
        private const string MqttTopicLocationLat = "Gps_sensor_lat";
        private const string MqttTopicLocationLon = "Gps_sensor_lon";

        // Properties om laatste sensorwaarden op te slaan
        public string LatestSound { get; private set; }
        public string LatestTemperature { get; private set; }
        public string LatestGasDetection { get; private set; }
        public string LatestLatitude { get; private set; }
        public string LatestLongitude { get; private set; }
        public string LatestHeartbeat { get; private set; }
        public string LatestZuurstof { get; private set; }


        public MqttService(MainPage mainPage, DatabaseHelper databaseHelper)
        {
            main = mainPage;
            _databaseHelper = databaseHelper;
            Debug.WriteLine("Start");
            ConnectMqtt();
        }

        private void ConnectMqtt()
        {
            Console.WriteLine("Connecting");
            try
            {
                _mqttClient = new MqttClient(
                    BrokerAddress,
                    BrokerPort,
                    true, // Enable SSL/TLS
                    MqttSslProtocols.TLSv1_2, // Use TLS 1.2
                    null, // No certificate validation callback (default)
                    null // No client certificates
                );

                _mqttClient.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;

                string clientId = Guid.NewGuid().ToString();
                _mqttClient.Connect(clientId, MqttUser, MqttPassword);

                _mqttClient.Subscribe(new string[]
                    {
                        MqttTopicGasDetection,
                        MqttTopicSound,
                        MqttTopicTemperature,
                        MqttTopicLocationLat,
                        MqttTopicLocationLon,
                        MqttTopicZuurstof,
                        MqttTopicHartslag
                    },
                    new byte[]
                    {
                        MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                        MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                        MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                        MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                        MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                        MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                        MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE
                    });

                Console.WriteLine("MQTT connected and subscribed with TLS.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("MQTT connection error: " + ex.Message);
            }
        }

        public event Action<string>? TemperatureUpdated;
        public event Action<string>? HeartbeatUpdated;
        public event Action<string>? ZuurstofUpdated;
        public event Action<string>? DirectionUpdated;
        public event Action<string>? GasUpdated;

        private void Client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            var topic = e.Topic;
            var message = Encoding.UTF8.GetString(e.Message);

            Console.WriteLine($"MQTT message received: Topic={topic}, Message={message}");

            if (topic == "sensor/temperature")
            {
                TemperatureUpdated?.Invoke(message);
            }

            if (topic == "sensor/heartbeat")
            {
                HeartbeatUpdated?.Invoke(message);
            }
            
            if (topic == "sensor/zuurstof")
            {
                ZuurstofUpdated?.Invoke(message);
            }

            if (topic == "sensor/geluid")
            {
                DirectionUpdated?.Invoke(message);
            }

            if (topic == "sensor/gas")
            {
                GasUpdated?.Invoke(message);
            }

            // Update properties
            switch (topic)
            {
                case MqttTopicSound:
                    LatestSound = message;
                    DirectionUpdated?.Invoke(message);
                    break;

                case MqttTopicTemperature:
                    LatestTemperature = message;
                    Console.WriteLine($"[Temperature] {message}");
                    TemperatureUpdated?.Invoke(message); // event aanroepen
                    break;

                case MqttTopicGasDetection:
                    LatestGasDetection = message;
                    GasUpdated?.Invoke(message);
                    break;

                case MqttTopicLocationLat:
                    LatestLatitude = message;
                    break;

                case MqttTopicLocationLon:
                    LatestLongitude = message;
                    break;

                case MqttTopicZuurstof:
                    LatestZuurstof = message;
                    ZuurstofUpdated?.Invoke(message);
                    break;

                case MqttTopicHartslag:
                    LatestHeartbeat = message;
                    HeartbeatUpdated?.Invoke(message); // event aanroepen
                    break;

                default:
                    Console.WriteLine("Unknown MQTT topic received.");
                    break;
            }

            // Data opslaan in database
            SaveToDatabase(message, topic);
        }

        private async void SaveToDatabase(string message, string topic)
        {
            try
            {
                var timestamp = DateTime.UtcNow.AddHours(2).ToString("yyyy-MM-dd HH:mm:ss");

                // Sensorwaarden gaan naar de Modules-tabel
                if (topic == MqttTopicTemperature || topic == MqttTopicHartslag || topic == MqttTopicZuurstof || topic == MqttTopicSound || topic == MqttTopicGasDetection)
                {
                    var newModule = new Modules
                    {
                        Timestamp = timestamp
                    };

                    // Probeer de waarde te parsen en in de database te zetten
                    if (topic == MqttTopicTemperature)
                    {
                        if (float.TryParse(message, NumberStyles.Float, CultureInfo.InvariantCulture, out float tempValue))
                        {
                            newModule.Temperatuur = tempValue;
                        }
                        else
                        {
                            Console.WriteLine("Kon temperatuur niet parsen: " + message);
                            return;
                        }
                    }
                    else if (topic == MqttTopicHartslag)
                    {
                        if (int.TryParse(message, out int hartslagValue))
                        {
                            newModule.Hartslag = hartslagValue;
                        }
                        else
                        {
                            Console.WriteLine("Kon hartslag niet parsen: " + message);
                            return;
                        }
                    }
                    else if (topic == MqttTopicZuurstof)
                    {
                        if (int.TryParse(message, out int zuurstofValue))
                        {
                            newModule.Zuurstof = zuurstofValue;
                        }
                        else
                        {
                            Console.WriteLine("Kon zuurstof niet parsen: " + message);
                            return;
                        }
                    }
                    else if (topic == MqttTopicSound)
                    {
                        if (float.TryParse(message, NumberStyles.Float, CultureInfo.InvariantCulture, out float directionValue))
                        {
                            newModule.Direction = directionValue;
                        }
                        else
                        {
                            Console.WriteLine("Kon directie niet parsen: " + message);
                            return;
                        }
                    }
                    else if (topic == MqttTopicGasDetection)
                    {
                        if (int.TryParse(message, out int gasValue))
                        {
                            newModule.Gas = gasValue;
                        }
                        else
                        {
                            Console.WriteLine("Kon gas niet parsen: " + message);
                            return;
                        }
                    }

                        await _databaseHelper.SaveModulesAsync(newModule);
                    Console.WriteLine("Sensor data opgeslagen in Modules tabel.");
                }
                // GPS-gegevens gaan naar de Paden-tabel
                else if (topic == MqttTopicLocationLat || topic == MqttTopicLocationLon)
                {
                    var newPaden = new Paden
                    {
                        Timestamp = timestamp
                    };

                    if (topic == MqttTopicLocationLat)
                        newPaden.LocationLat = message;
                    else if (topic == MqttTopicLocationLon)
                        newPaden.LocationLon = message;

                    await _databaseHelper.SavePadenAsync(newPaden);
                    Console.WriteLine("GPS data opgeslagen in Paden tabel.");
                }
                else
                {
                    Console.WriteLine("Onbekend topic: " + topic);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fout bij opslaan in database: " + ex.Message);
            }
        }
    }
}