using Delivery_Link.Types;
using System;
using System.Diagnostics;
using System.Media;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Delivery_Link
{

    public class RecieveThread  // Class to hold the home window and connection info
    {
        private int messageIndexNumber = 1;
        private ConnectionInfo connectionInformation;
        private Home homeWindow;

        public RecieveThread(ConnectionInfo connectionInformation, Home homeWindow)
        {
            this.connectionInformation = connectionInformation;
            this.homeWindow = homeWindow;
        }


        public void RecieveThreadMethod()   // loop to recieve thread
        {
            while (true)
            {
                // Calls HTTP request
                GetHTTP();
                Thread.Sleep(15000);
            }
        }

        private Button CreateButton(string objectName, string type)
        {
            Button button = new Button(); // The button to hold the textblock
            button.Background = new SolidColorBrush(Colors.Transparent);
            button.BorderThickness = new Thickness(0);
            button.Cursor = Cursors.Hand;

            //  Callsign_Number will be used to reference messages.
            string name = objectName;
            button.Name = name;


            messageIndexNumber++;

            if (type == "message")
            {
                button.MouseRightButtonDown += homeWindow.DeleteMessage;
                button.Click += homeWindow.SelectMessage;
            }
            else if (type == "login")
            {
                button.MouseRightButtonDown += homeWindow.DeleteAircraft;
                button.Click += homeWindow.SelectAircraft;
            }

            Style messageStyle = homeWindow.FindResource("MessageButton") as Style;
            button.Style = messageStyle;

            return button;

        }

        private TextBlock CreateTextBlock(string time, string callsign, string content, string type)
        {

            TextBlock tb = new TextBlock(); // A new textblock to hold the message

            // Properties

            tb.Padding = new Thickness(10, 0, 0, 0);
            tb.Margin = new Thickness(0, 0, 0, 4);

            tb.Cursor = Cursors.Hand;   // Hand cursor

            Color customGrayColor = (Color)ColorConverter.ConvertFromString("#FFE2E2E2");   // Custom color
            SolidColorBrush customGrayBrush = new SolidColorBrush(customGrayColor);
            tb.Foreground = customGrayBrush;

            tb.FontSize = 17;
            tb.FontFamily = new FontFamily("Yu Gothic UI Light");

            tb.TextWrapping = TextWrapping.Wrap;
            tb.VerticalAlignment = VerticalAlignment.Top;
            tb.HorizontalAlignment = HorizontalAlignment.Left;
            tb.Width = 596;

            switch (type)
            {
                case "message":
                    tb.Text = $"{time}    {callsign}    {content}";
                    break;
                case "logon":
                    tb.Text = callsign;
                    break;
            }

            return tb;
        }


        public void GetHTTP()
        {
            // HTTP request and decoding
            HttpWebRequest request = WebRequest.Create($"http://www.hoppie.nl/acars/system/connect.html?logon={connectionInformation.loginCode}&from={connectionInformation.callsign}&to=any&type=poll&packet=") as HttpWebRequest;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            WebHeaderCollection header = response.Headers;
            var encoding = ASCIIEncoding.ASCII;
            using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
            {
                string responseText = reader.ReadToEnd();
                if (responseText == "ok ")  // "ok " is recieved when there are no messages. Ignore the packet if there are no messages.
                {
                    // No new messages
                }
                else
                {
                    // New messages recieved.
                    // Remove \n and seperate into an array
                    string cleanResponse = responseText.Replace("\n", " ").Replace("\r", "").Replace("ok ", "").Replace("}} {", "}};{");
                    int objectCount = Regex.Matches(cleanResponse, ";").Count + 1;
                    string[] messages = cleanResponse.Split(';');

                    // Current Time
                    DateTime currentDate = DateTime.UtcNow;
                    string currentTime = currentDate.ToString("HHmm");

                    bool noisePlayed = false;

                    for (int i = 0; i < messages.Length; i++) // Turn into an incomming message object
                    {
                        Trace.WriteLine(messages[i]);
                        Trace.WriteLine(cleanResponse + "\n");
                        IncommingMessage deliveryMessage = new IncommingMessage();


                        string[] split = messages[i].Split('{');

                        deliveryMessage.timeRecieved = currentTime;
                        deliveryMessage.type = split[1].Split(' ')[1];
                        deliveryMessage.callsign = split[1].Split(' ')[0];
                        deliveryMessage.content = split[2].Replace("}}", "");

                        // Check if it is a logon request
                        if (deliveryMessage.content.Contains("LOGOFF"))
                        {
                            homeWindow.Dispatcher.Invoke(() =>
                            {
                                homeWindow.DeleteAircraft(deliveryMessage.callsign);
                            });
                        }
                        if (deliveryMessage.content.Contains("REQUEST LOGON"))
                        {
                            bool repeatedLogon = false;

                            homeWindow.Dispatcher.Invoke(() =>
                            {
                                if (homeWindow.OnlineAircraftList.FindName(deliveryMessage.callsign) == null)
                                {
                                   
                                    TextBlock aircraftTextblock = CreateTextBlock(deliveryMessage.timeRecieved, deliveryMessage.callsign, deliveryMessage.content, "logon");

                                    Button button = CreateButton(deliveryMessage.callsign, "login");

                                    button.Content = aircraftTextblock;

                                    homeWindow.OnlineAircraftList.RegisterName(button.Name, button);
                                    homeWindow.OnlineAircraftList.Children.Add(button);
                                }
                                else
                                {
                                    repeatedLogon = true;
                                }
                            });

                            homeWindow.SendMessageToServer(deliveryMessage.callsign, "/data2/18/3/NE/LOGON ACCEPTED", "cpdlc");

                            if (repeatedLogon)
                            {

                            } else if (noisePlayed == false)
                            {
                                var notificationSound = new SoundPlayer(Properties.Resources.Notification);
                                notificationSound.PlaySync();
                                noisePlayed = true;
                            }

                        }
                        else
                        {
                            if (deliveryMessage.content.Contains("LOGOFF")) { } else
                            {                                
                                homeWindow.Dispatcher.Invoke(() => // Required to modify GUI from external class
                                {
                                    TextBlock deliveryTextBlock = CreateTextBlock(deliveryMessage.timeRecieved, deliveryMessage.callsign, deliveryMessage.content, "message"); // Textblock with properties + text

                                    string name = $"{deliveryMessage.callsign}_" + messageIndexNumber.ToString();
                                    Button button = CreateButton(name, "message");

                                    button.Content = deliveryTextBlock;

                                    homeWindow.InboundMessageLogPanel.RegisterName(button.Name, button);
                                    homeWindow.InboundMessageLogPanel.Children.Add(button);


                                });
                                if (noisePlayed == false)
                                {
                                    var notificationSound = new SoundPlayer(Properties.Resources.Notification);
                                    notificationSound.PlaySync();
                                    noisePlayed = true;
                                }
                            }
                        }
                    }
                }
            }
        }


    }
}
