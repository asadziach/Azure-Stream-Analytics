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
using Newtonsoft.Json;
using SimHeater;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Microsoft.Azure.Devices.Client.Samples
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
        internal void showReceived(String data) {
            recv.Text += data + "\r\n";
        }
        private void buttonClicked(object sender, RoutedEventArgs e)
        {
           SensorEvent sEvent = new SensorEvent();
            sEvent.deviceid = "Heater1";
            sEvent.currenttemp = Int32.Parse(tempval.Text);
            sEvent.gassense = Int32.Parse(gasval.Text);
            sEvent.flamesense = Int32.Parse(flameval.Text);
            sEvent.humidity = Int32.Parse(humidityval.Text);
            sEvent.status = statustext.Text;
            String jsongString = JsonConvert.SerializeObject(sEvent);
            IoTClient.SendEvent(jsongString);
        }
    }
}
