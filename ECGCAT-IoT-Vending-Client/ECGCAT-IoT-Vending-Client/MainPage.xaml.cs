/**
 * This is a port of a project from hackster.io
 *https://www.hackster.io/LaurenBuchholz/restful-weather-5bc747
**/
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ECGCAT_IoT_Vending_Client
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const int LED_PIN = 12;
        private GpioPin pin;
        private GpioPinValue pinValue;
        private DispatcherTimer LoggerTimer;
        private bool go = false;
        private string serverPort;
        private string serverIPURL;
        //A class which wraps the color sensor
        TCS34725 colorSensor;
        //A class which wraps the barometric sensor
        BMP280 BMP280;
        
        private const string I2C_CONTROLLER_NAME = "I2C1"; //use for RPI2
        
        public MainPage()
        {
            this.InitializeComponent();

            RetreiveAppStore();

            LoggerTimer = new DispatcherTimer();
            LoggerTimer.Interval = TimeSpan.FromSeconds(3); //take data every 3 seconds
            LoggerTimer.Tick += LoggerTimer_Tick;

            InitGPIO();

            if (pin != null)
            {
                LoggerTimer_Tick(this, null);
                LoggerTimer.Start();
            }
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                //GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            pin = gpio.OpenPin(LED_PIN);
            pinValue = GpioPinValue.High;
            pin.Write(pinValue);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            //GpioStatus.Text = "GPIO pin initialized correctly.";

        }

        private void LoggerTimer_Tick(object sender, object e)
        {
            if (pinValue == GpioPinValue.High)
            {
                pinValue = GpioPinValue.Low;
                pin.Write(pinValue);
                ReadWeatherData().ContinueWith((t) =>
                {
                    SensorData sd = t.Result;
                    Kafka kafka = new Kafka();

                    Debug.WriteLine(sd.Created);
                    //Write the values to your debug console
                    Debug.WriteLine("Created: " + sd.Created + " ft");
                    time.Text = sd.Created.ToString("g");
                    Debug.WriteLine("Temperature: " + sd.TemperatureinF + " deg F");
                    temperature.Text = (sd.TemperatureinF.ToString("F2") + "°F");
                    Debug.WriteLine("Pressure: " + sd.Pressureinmb + " mb");
                    pressure.Text = (sd.Pressureinmb.ToString("F0") + " mb");

                    if (go == true)
                    {
                        Task.Run(() => kafka.PostDataAsync(sd, "SensorData", serverIPURL, serverPort)); //fire and forget for now
                    }

                }, TaskScheduler.FromCurrentSynchronizationContext());
                ReadLightData().ContinueWith((t) =>
                {
                    LightData ld = t.Result;

                    //enable at future date
                    //Kafka kafka = new Kafka();

                    Debug.WriteLine("Lux: " + ld.Lux);
                    Debug.WriteLine("Color Temp:" + ld.ColorTempinK + " K");
                    lux.Text = (ld.Lux.ToString("F0") + " lx");

                    //enable at future date (see above as well)
                    //if (go == true)
                    //{
                    //    Task.Run(() => kafka.PostDataAsync(sd, "SensorData")); //fire and forget for now
                    //}

                }, TaskScheduler.FromCurrentSynchronizationContext());
                
                pinValue = GpioPinValue.High;
                pin.Write(pinValue);
            }
            else
            {
                pinValue = GpioPinValue.High;
                pin.Write(pinValue);
            }
        }

        private async Task<SensorData> ReadWeatherData()
        {
            SensorData sd = null;
            try
            {
                if (BMP280 == null)
                {
                    //Create a new object for our barometric sensor class
                    BMP280 = new BMP280();
                    //Initialize the sensor
                    await BMP280.Initialize();
                }

                //Create variables to store the sensor data: temperature, pressure and altitude. 
                //Initialize them to 0.
                float temp = 0;
                float pressure = 0;
                float altitude = 0;

                //Create a constant for pressure at sea level. 
                //This is based on your local sea level pressure (Unit: Hectopascal)
                const float seaLevelPressure = 1018.34f;

                temp = await BMP280.ReadTemperature();
                temp = ConvertUnits.ConvertCelsiusToFahrenheit(temp);
                pressure = await BMP280.ReadPreasure();
                pressure = ConvertUnits.ConvertPascalToMillibar(pressure);
                altitude = await BMP280.ReadAltitude(seaLevelPressure);
                altitude = ConvertUnits.ConvertMeterToFoot(altitude);

                sd = new SensorData();
                sd.Created = DateTime.Now;
                sd.TemperatureinF = temp;
                sd.Pressureinmb = pressure;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return sd;
        }

        private async Task<LightData> ReadLightData()
        {
            LightData ld = null;
            try
            {
                if (colorSensor == null)
                {
                    colorSensor = new TCS34725();
                    await colorSensor.Initialize();
                }

                //Read the approximate color from the sensor
                string colorRead = await colorSensor.getClosestColor();
                
                RgbData rgb = await colorSensor.getRgbData();

                float lux = TCS34725.getLuxSimple(rgb);
                ld = new LightData();
                ld.Created = DateTime.Now;
                ld.rgbData = rgb;
                ld.Lux = lux;
                ld.ColorTempinK = TCS34725.calculateColorTemperature(rgb);
                Debug.WriteLine("Current lux: " + lux);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return ld;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (serverIPURL == null && serverPort ==null) //there is no local app settings store value for port and URL, we need to get one
            { 
                if (IpUrlBox.Text == "" && PortBox.Text == "") //you didn't fill out the boxes, and there were no previously stored settings
                {
                    logArea.Text = "You need to add an IP/URL and Port number for the server";
                    go = false; 
                }
                else // you did fill out the boxes and we're grabbing the settings
                {
                    logArea.Text = "";
                    serverIPURL = IpUrlBox.Text;
                    serverPort = PortBox.Text;
                    SetAppStore(serverIPURL, serverPort);
                    go = true;
                }
            }
            else //we pulled the previously stored values from the local app settings store
            {
                if(serverIPURL != IpUrlBox.Text || serverPort != PortBox.Text) //the local settings and current input text don't match
                {
                    serverIPURL = IpUrlBox.Text; //reset the settings to match the input text
                    serverPort = PortBox.Text;
                    SetAppStore(serverIPURL, serverPort);
                    go = true;
                }
                else //the locat settings and input settings match. Go. 
                {
                    go = true;
                }
                
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            go = false;
        }

        private void AppSettings_Click(object sender, RoutedEventArgs e)
        {
            AppMenu.IsPaneOpen = !AppMenu.IsPaneOpen;
            if (OfflineMode.IsOn == false)
            {
                SettingsToggleOff();
            }
        }

        private void OfflineMode_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggleSwitch = sender as ToggleSwitch;
            if (toggleSwitch !=null)
            {
                if (toggleSwitch.IsOn == true)
                {
                    SettingsToggleOn();
                }
                else
                {
                    SettingsToggleOff();
                    
                }
            }
        }
        private void SettingsToggleOn()
        {
            IpTextBlock.Visibility = Visibility.Visible;
            IpUrlBox.Visibility = Visibility.Visible;
            PortTextBlock.Visibility = Visibility.Visible;
            PortBox.Visibility = Visibility.Visible;
        }
        private void SettingsToggleOff()
        {
            IpTextBlock.Visibility = Visibility.Collapsed;
            IpUrlBox.Visibility = Visibility.Collapsed;
            PortTextBlock.Visibility = Visibility.Collapsed;
            PortBox.Visibility = Visibility.Collapsed;
        }
        private void RetreiveAppStore()
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;

            object portValue = localSettings.Values["serverPort"];
            if (portValue != null)
            {
                serverPort = portValue.ToString();
            }

            object ipValue = localSettings.Values["ipAddress"];
            if (ipValue != null)
            { 
                serverIPURL = ipValue.ToString();
            }

        }
        private void SetAppStore(string _ipString, string _portString)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            localSettings.Values["serverPort"] = _portString;
            localSettings.Values["ipAddress"] = _ipString;
        }
    }
}
