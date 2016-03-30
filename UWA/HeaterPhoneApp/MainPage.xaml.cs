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

using ppatierno.AzureSBLite;
using ppatierno.AzureSBLite.Messaging;
using System;
using System.IO;
using System.Text;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace HeaterPhoneApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static string SB_CONNECTION_STRING = "Endpoint=sb://ihsuprodhkres002dednamespace.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=Wetz9xvqpyaCw0W/xXiXGuOoRTCtJbh6vU/7+zIoz80=";
        static string EVENT_HUB_NAME = "iothub-ehub-smartheate-22847-cde4b71e03";
        static string EVENT_HUB_PARTITION_ID = "0";

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            DateTime startingDateTimeUtc = DateTime.Now.Subtract(new TimeSpan(1,0,0) );// new DateTime(2016, 3, 29, 1, 42, 00);

            ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder(SB_CONNECTION_STRING);
            builder.TransportType = TransportType.Amqp;

            MessagingFactory factory = MessagingFactory.CreateFromConnectionString(SB_CONNECTION_STRING);

            EventHubClient client = factory.CreateEventHubClient(EVENT_HUB_NAME);
            EventHubConsumerGroup group = client.GetConsumerGroup("Controllers");

            EventHubReceiver receiver = group.CreateReceiver(EVENT_HUB_PARTITION_ID, startingDateTimeUtc);

            System.Diagnostics.Debug.WriteLine("Listening...");
            for (int i = 0; i < 1; i++)
            {
                EventData data = receiver.Receive();
                if (data == null)
                {
                    System.Diagnostics.Debug.WriteLine("data null");
                    break;
                }
                else {
                    System.Diagnostics.Debug.WriteLine("{0} {1} {2}", data.PartitionKey, data.EnqueuedTimeUtc.ToLocalTime(), Encoding.UTF8.GetString(data.GetBytes(), 0, data.GetBytes().Length));

                }
            }
            /*
            System.Diagnostics.Debug.WriteLine("connection closing");
            receiver.Close();

            System.Diagnostics.Debug.WriteLine("receiver.Close");
            client.Close();

            System.Diagnostics.Debug.WriteLine("client.Close");
            factory.Close();
            System.Diagnostics.Debug.WriteLine("connection closed");*/
        }
    }
}
