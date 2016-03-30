// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Samples;

namespace SimHeater
{
    class IoTClient
    {
        private static int MESSAGE_COUNT = 5;

        // String containing Hostname, Device Id & Device Key in one of the following formats:
        //  "HostName=<iothub_host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"
        //  "HostName=<iothub_host_name>;CredentialType=SharedAccessSignature;DeviceId=<device_id>;SharedAccessSignature=SharedAccessSignature sr=<iot_host>/devices/<device_id>&sig=<token>&se=<expiry_time>";
        private const string DeviceConnectionString = "HostName=HeaterIotS1.azure-devices.net;DeviceId=Heater1;SharedAccessKey=qAwaMlXRuRuriTz0VIhCK0Jn6bbAQHO9YPhIO2EWmmQ=";
        private static MainPage page;
        private static DeviceClient deviceClient;
        public async static Task Start(MainPage p)
        {
            page = p;
            try
            {
                deviceClient = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Http1);

                //await SendEvent(deviceClient);
                await ReceiveCommands();

                Debug.WriteLine("Exited!\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        internal static async Task SendEvent(String json)
        {
   
            Debug.WriteLine("Device sending {0} messages to IoTHub...\n", MESSAGE_COUNT);
            Message eventMessage = new Message(Encoding.UTF8.GetBytes(json));
            deviceClient.SendEventAsync(eventMessage);
        }

        static async Task ReceiveCommands()
        {
            Debug.WriteLine("\nDevice waiting for commands from IoTHub...\n");
            Message receivedMessage;
            string messageData;

            while (true)
            {
                receivedMessage = await deviceClient.ReceiveAsync();

                if (receivedMessage != null)
                {
                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Debug.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);
                    page.showReceived(messageData);
                    await deviceClient.CompleteAsync(receivedMessage);
                }

                //  Note: In this sample, the polling interval is set to 
                //  10 seconds to enable you to see messages as they are sent.
                //  To enable an IoT solution to scale, you should extend this //  interval. For example, to scale to 1 million devices, set 
                //  the polling interval to 25 minutes.
                //  For further information, see
                //  https://azure.microsoft.com/documentation/articles/iot-hub-devguide/#messaging
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}