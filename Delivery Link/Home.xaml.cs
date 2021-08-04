using Delivery_Link.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Diagnostics;
using System.Collections.Generic;
using System.Configuration;
using System.Collections.Specialized;

namespace Delivery_Link
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>

    public partial class Home : Page
    {

        private string SelectedAircraft;
        ConnectionInfo connectionInformation = new ConnectionInfo();

        public string GetAircraft()
        {
            return SelectedAircraft;
        }

        public void SetAircraft(string newAircraft, string callsign)
        {
            SelectedAircraft = newAircraft;
            AircraftCallsign.Text = callsign;

        }

        public void SetAircraftData(PilotData ad)
        {
            if (ad.callsignFound == true)   // Add the information
            {
                AircraftTypeBox.Text = ad.flighPlan.aircraft_short;
                DepartureBox.Text = ad.flighPlan.departure;
                ArrivalBox.Text = ad.flighPlan.arrival;
                CruiseBox.Text = ad.flighPlan.altitude;
                SquawkBox.Text = ad.transponder;
                RouteBox.Text = ad.flighPlan.route;
            } 
            else    // Set info to blank
            {
                AircraftTypeBox.Text = string.Empty;
                DepartureBox.Text = string.Empty;
                ArrivalBox.Text = string.Empty;
                CruiseBox.Text = string.Empty;
                SquawkBox.Text = string.Empty;
                RouteBox.Text = string.Empty;
            }
        }

        public void RemoveAircraftData()
        {
            AircraftCallsign.Text = string.Empty;
            AircraftTypeBox.Text = string.Empty;
            DepartureBox.Text = string.Empty;
            ArrivalBox.Text = string.Empty;
            CruiseBox.Text = string.Empty;
            SquawkBox.Text = string.Empty;
            RouteBox.Text = string.Empty;
            TelexBox.Text = string.Empty;
        }

        public Home(ConnectionInfo incommingConnectionInformation)
        {
            InitializeComponent();

            connectionInformation = incommingConnectionInformation;

            // Begin recieving messages from server
            RecieveThread recieveObject = new RecieveThread(connectionInformation, this);
            Thread recieveThread = new Thread(new ThreadStart(recieveObject.RecieveThreadMethod));
            recieveThread.IsBackground = true;
            recieveThread.Start();
        }

        public void RefreshAircraftData(object sender, RoutedEventArgs e)
        {
            string callsign = AircraftCallsign.Text;

            RecieveVatsimData(callsign);
        }

        public void SelectMessage(object sender, RoutedEventArgs e)
        {
            Trace.WriteLine('1');
            Button message = (Button)sender;
            string callsign = message.Name.Split('_')[0];

            Trace.WriteLine('2');

            SetAircraft(message.Name, callsign);

            Trace.WriteLine('3');

            RecieveVatsimData(callsign);


        }

        public void DeleteMessage(object sender, RoutedEventArgs e)
        {
            Button message = (Button)sender;
            string messageName = message.Name;
            
            // If the currently selected message is the one we want to delete, remove it's data
            if (GetAircraft() == messageName)
            {
                SetAircraft("","");
                RemoveAircraftData();
            }

            // Delete the message
            InboundMessageLogPanel.Children.Remove(message);

        }

        public async System.Threading.Tasks.Task RecieveVatsimData(string callsign)
        {
            VatsimData vatsimData = new VatsimData();
            Thread aircraftDataThread = new Thread(async () => await vatsimData.AircraftDataRequest(callsign, this));
            aircraftDataThread.IsBackground = true;
            aircraftDataThread.Start();

        }

        public void pushVatsimData(PilotData aircraftData)
        {

            SetAircraftData(aircraftData);
        }

        public void SelectMessage()
        {
            //empty
        }


        public void SendTelexMessage(object sender, RoutedEventArgs e)
        {
            string callsign = AircraftCallsign.Text;
            string telexMessage = TelexBox.Text;

            SendMessageToServer(callsign, telexMessage);


        }

        public string convertAltitude(string alt)
        {
            if (alt.Contains("000"))
            {
                string flightLevel = alt.Split(new string[] { "000" }, System.StringSplitOptions.None)[0];
                if (int.Parse(flightLevel) >= 18)
                {
                    return "FL" + flightLevel + "0";
                } else
                {
                    return alt + "FT";
                }
            } else if (alt.Contains("500"))
            {
                string flightLevel = alt.Split(new string[] { "500" }, System.StringSplitOptions.None)[0];
                if (int.Parse(flightLevel) >= 18)
                {
                    return "FL" + flightLevel + "5";
                } else
                {
                    return alt + "FT";
                }
            } else
            {
                return alt + "FT";
            }
        }

        public void SendMessage(object sender, RoutedEventArgs e)
        {
            string callsign = AircraftCallsign.Text;
            string aircraft = AircraftTypeBox.Text; 
            string departure = DepartureBox.Text;
            string arrival = ArrivalBox.Text;
            string initial = InitialBox.Text;
            string cruise = convertAltitude(CruiseBox.Text);
            string squawk = SquawkBox.Text;
            string route = RouteBox.Text;
            string gndFreq = DepFreqBox.Text;
            string depFreq = GndFreqBox.Text;

            string procedure = route.Split(' ')[0];

            string telexFormat = ConfigurationManager.AppSettings.Get("telexFormat");

            telexFormat = telexFormat.Replace("@callsign", callsign);
            telexFormat = telexFormat.Replace("@departure", departure);
            telexFormat = telexFormat.Replace("@arrival", arrival);
            telexFormat = telexFormat.Replace("@cruise", cruise);
            telexFormat = telexFormat.Replace("@route", route);
            telexFormat = telexFormat.Replace("@procedure", procedure);
            telexFormat = telexFormat.Replace("@initial", initial);
            telexFormat = telexFormat.Replace("@depFreq", depFreq);
            telexFormat = telexFormat.Replace("@gndFreq", gndFreq);
            telexFormat = telexFormat.Replace("@squawk", squawk);
            telexFormat = telexFormat.Replace("\\n", "\n");
           
            Trace.WriteLine(telexFormat);


            SendMessageToServer(callsign, telexFormat);


        }

        public async System.Threading.Tasks.Task SendMessageToServer(string callsign, string packet)
        {
            HttpClient sendMessageClient = new HttpClient();

            var values = new Dictionary<string, string>
                {
                    { "logon", connectionInformation.loginCode },
                    { "from", connectionInformation.callsign },
                    { "to", callsign },
                    { "type", "telex" },
                    { "packet", packet },

                };

            var content = new FormUrlEncodedContent(values);

            await sendMessageClient.PostAsync("http://www.hoppie.nl/acars/system/connect.html", content);

            // Delete message log of aircraft
            object button = InboundMessageLogPanel.FindName(GetAircraft());
            if (button is Button)
            {
                Button buttonToDelete = button as Button;
                InboundMessageLogPanel.Children.Remove(buttonToDelete);
            }
            RemoveAircraftData();
        }

        private string selectedMode = "PDC";


        public void SwitchMessageMode(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            string selectedButton = button.Name;
            
            if (selectedButton == selectedMode)
            {
                // Mode already selected
            } else
            {

                Style greenStyle = this.FindResource("GreenButton") as Style;
                Style grayStyle = this.FindResource("GrayButton") as Style;

                switch (selectedMode)
                {
                    case "PDC":
                        selectedMode = "TELEX";

                        PDCGrid.Visibility = Visibility.Hidden;
                        TelexGrid.Visibility = Visibility.Visible;

                        TelexButton.Style = greenStyle;
                        PDCButton.Style = grayStyle;
                        break;
                    case "TELEX":
                        selectedMode = "PDC";

                        TelexGrid.Visibility = Visibility.Hidden;
                        PDCGrid.Visibility = Visibility.Visible;

                        PDCButton.Style = greenStyle;
                        TelexButton.Style = grayStyle;
                        break;
                }
                    
            }

        }
    }
}

