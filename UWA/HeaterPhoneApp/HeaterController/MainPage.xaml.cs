using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Azure.Devices.Client;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Azure.Devices.Client.Samples;
using Windows.UI.Popups;
using NotificationsExtensions.Toasts;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI.Notifications;
using Newtonsoft.Json;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HeaterController
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
  
        public MainPage()
        {
            this.InitializeComponent();
#pragma warning disable 4014
            IoTClient.Start(this);
#pragma warning restore 4014

        }

        internal void updateTelemetry(SensorEvent e)
        {
            temptextBlock.Text = e.currenttemp.ToString();
            humiditytextBlock.Text = e.humidity.ToString();

            string status = e.status;
            if (status != null) { 
                if (status.StartsWith("ALARM"))
                {
                    var dialog = new MessageDialog(status);
                    dialog.ShowAsync();
                }
                else if (status.Equals("ON"))
                {
                    toggleSwitch.IsOn = true;
                }

                if (status.Contains("OFF"))
                {
                    toggleSwitch.IsOn = false;
               }
            }
        }

        private void send_Click(object sender, RoutedEventArgs e)
        {
            // SendDeviceToCloudMessagesAsync();
            //SendEvent(deviceClient);
        }

        private void receive_Click(object sender, RoutedEventArgs e)
        {
            //ReceiveCommands(deviceClient);
        }

        private void SendToastNotification()
        {
            ToastContent content = new ToastContent()
            {
                Launch = "lei",

                Visual = new ToastVisual()
                {
                    TitleText = new ToastText()
                    {
                        Text = "Connecting...."
                    },

                    BodyTextLine1 = new ToastText()
                    {
                        Text = "Sending new command to heater"
                    },

                },
            };


            DataPackage dp = new DataPackage();
            dp.SetText(content.GetContent());

            ToastNotificationManager.CreateToastNotifier().Show(new ToastNotification(content.GetXml()));
        }

        private void toggleHandler(object sender, RoutedEventArgs e)
        {
            SendToastNotification();

            //TODO disable receiving new commands while this command gets to device
            SendToastNotification();
            int param = 0;

            HeaterCommand heatercommand = new HeaterCommand();
            string command;
            if (toggleSwitch.IsOn)
            {
                heatercommand.command = "TurnHeaterOn";
            }
            else {
                heatercommand.command = "TurnHeaterOff";
                heatercommand.parameterName = "reason";
                heatercommand.parameter = param;
            }
            command = JsonConvert.SerializeObject(heatercommand);
            IoTClient.SendEvent(command);
        }

        private void tempValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            SendToastNotification();
            int newTemp = (int)e.NewValue;

            HeaterCommand heatercommand = new HeaterCommand();
            heatercommand.command = "SetDesiredTemp";
            heatercommand.parameterName = "temperature";
            heatercommand.parameter = newTemp;

            String command = JsonConvert.SerializeObject(heatercommand);

            settemp.Text = "Desired Temp: " + newTemp + "C"; 

            IoTClient.SendEvent(command);
        }
    }
}
