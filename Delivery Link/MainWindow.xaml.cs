using Delivery_Link.Types;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Navigation;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using Delivery_Link.Properties;

namespace Delivery_Link
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml``
    /// </summary>
    /// 
    

    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();
            CheckVersion();

            loginCode.Password = Settings.Default.Login;
            callsign.Text = Settings.Default.Callsign;

        }

        public async System.Threading.Tasks.Task CheckVersion()
        {
            bool checkForUpdates = true;

            if (checkForUpdates)
            {
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.Add("User-Agent", "request");
                string version = await client.GetStringAsync("https://api.github.com/repos/migte/Delivery-Link/releases/latest");
                client.DefaultRequestHeaders.Remove("User-Agent");

                dynamic jsonResponse = JsonConvert.DeserializeObject(version); // string -> object
                JObject jsonObject = (JObject)jsonResponse;

                foreach (JProperty property in jsonObject.Properties())
                {
                    string field = property.Name;
                    string latestVersion = property.Value.ToString();
                    string currentVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;

                    if (field == "tag_name")
                    {
                        if (currentVersion == latestVersion)
                        {
                            // Latest version already installed.   
                        }
                        else
                        {
                            // Update available.
                            CurrentVersionText.Text = "Current version: " + currentVersion;
                            LatestVersionText.Text = "Latest Version: " + latestVersion;

                            UpdateAvailableCanvas.Visibility = Visibility.Visible;
                        }
                    }
                }
            }

        }

        public void UpdateButton(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/migte/Delivery-Link/releases/latest");
        }

        public void LaterButton(object sender, RoutedEventArgs e)
        {
            UpdateAvailableCanvas.Visibility = Visibility.Hidden;
        }


        private void Login(object sender, RoutedEventArgs e)    // Triggered when login button is pressed
        {
            // Ensure both fields have text inputted.
            if (loginCode.Password != string.Empty && callsign.Text != string.Empty)    // Ensure neitehr field is empty
            {
                ConnectionInfo connectionInformation = new ConnectionInfo();    // Set connection information
                connectionInformation.loginCode = loginCode.Password;
                Trace.WriteLine(loginCode.Password);
                connectionInformation.callsign = callsign.Text;

                Settings.Default.Login = connectionInformation.loginCode;
                Settings.Default.Callsign = connectionInformation.callsign;
                Settings.Default.Save();

                // Ensures login code is functional. If so, it will run the rest of the program.
                checkLogin(connectionInformation);

            }
        }

        // Ensures login code is functional.
        public void checkLogin(ConnectionInfo connectionInformation)
        {
            // HTTP request
            HttpWebRequest request1 = WebRequest.Create($"http://www.hoppie.nl/acars/system/connect.html?logon={connectionInformation.loginCode}&from={connectionInformation.callsign}&to=AAL1234&type=poll&packet=") as HttpWebRequest;
            HttpWebResponse response1 = (HttpWebResponse)request1.GetResponse();
            WebHeaderCollection header1 = response1.Headers;
            var encoding1 = ASCIIEncoding.ASCII;

            using (var reader1 = new System.IO.StreamReader(response1.GetResponseStream(), encoding1))
            {
                // Converts response to text
                string responseText = reader1.ReadToEnd();

                // If true, login code is false
                if (responseText == "error {illegal logon code}")
                {
                    // Windows notification
                    new ToastContentBuilder()
                        .AddArgument("action", "viewConversation")
                        .AddArgument("conversationId", 9813)
                        .AddText("Login code failed.")
                        .AddText("Please enter a valid login code. You can create one at the Hoppie website.")
                        .Show(toast =>
                        {
                            // Ensures it will dissapear after a minute
                            toast.ExpirationTime = DateTime.Now.AddMinutes(1);
                        });
                } 
                // if true, callsign is already in use.
                else if (responseText == "error {callsign already in use}")
                {
                    // Windows notification
                    new ToastContentBuilder()
                        .AddArgument("action", "viewConversation")
                        .AddArgument("conversationId", 9813)
                        .AddText("Callsign In Use.")
                        .AddText("Your callsign is already in use.")
                        .Show(toast =>
                        {
                            // Ensures it will dissapear after a minute
                            toast.ExpirationTime = DateTime.Now.AddMinutes(1);
                        });
                } 
                else if (callsign.Text.Any(Char.IsLetter))  // Ensures atleast one letter is in use for the callsign.
                {
                    // Loads home page.
                    Home homePage = new Home(connectionInformation);
                    this.Width = 900;
                    this.Height = 620;
                    this.Content = homePage;
                }
                else    // General assumption that the callsign just doesn't work
                {
                    // Windows notification
                    new ToastContentBuilder()
                        .AddArgument("action", "viewConversation")
                        .AddArgument("conversationId", 9813)
                        .AddText("Callsign Invalid.")
                        .AddText("This callsign is invalid")
                        .Show(toast =>
                        {
                            // Ensures it will dissapear after a minute
                            toast.ExpirationTime = DateTime.Now.AddMinutes(1);
                        });
                }
            }
        }

    }

}
