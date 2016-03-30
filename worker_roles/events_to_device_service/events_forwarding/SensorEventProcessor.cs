using Microsoft.Azure.Devices;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TweetSharp;


namespace events_forwarding
{
    class SensorEventProcessor : IEventProcessor
    {
        Stopwatch checkpointStopWatch;
        PartitionContext partitionContext;
        DateTime lastTweet;

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Trace.TraceInformation(string.Format("EventProcessor Shuting Down.  Partition '{0}', Reason: '{1}'.", this.partitionContext.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }

        public Task OpenAsync(PartitionContext context)
        {
            Trace.TraceInformation(string.Format("Initializing EventProcessor: Partition: '{0}', Offset: '{1}'", context.Lease.PartitionId, context.Lease.Offset));
            this.partitionContext = context;
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            Trace.TraceInformation("\n");
            Trace.TraceInformation("........ProcessEventsAsync........");
            foreach (EventData eventData in messages)
            {
                try
                {
                    string jsonString = Encoding.UTF8.GetString(eventData.GetBytes());

                    Trace.TraceInformation(string.Format("Message received at '{0}'. Partition: '{1}'",
                        eventData.EnqueuedTimeUtc.ToLocalTime(), this.partitionContext.Lease.PartitionId));

                    Trace.TraceInformation(string.Format("-->Raw Data: '{0}'", jsonString));

                    SensorEvent newSensorEvent = this.DeserializeEventData(jsonString);

                    Trace.TraceInformation(string.Format("-->Serialized Data: '{0}', '{1}', '{2}', '{3}'",
                        newSensorEvent.deviceid, newSensorEvent.currenttemp, newSensorEvent.gassense, newSensorEvent.flamesense));
                    /* Send command to IoT device.*/
                    
                    // Turn off heater device.
                    string commandParameterNew = "{\"Name\":\"TurnHeaterOff\",\"Parameters\":{\"reason\":" + 1 + "}}";
                    Trace.TraceInformation("Turning heater off: '{0}'", newSensorEvent.deviceid);
                    Trace.TraceInformation("New Command Parameter: '{0}'", commandParameterNew);
                    await WorkerRole.iotHubServiceClient.SendAsync(newSensorEvent.deviceid, new Microsoft.Azure.Devices.Message(Encoding.UTF8.GetBytes(commandParameterNew)));
                    

                    if (DateTime.Now  - lastTweet > new TimeSpan(0, 5, 0)) // One tweet per 5 min
                    {
                        // Tweeting here
                        string consumerKey = ConfigurationManager.AppSettings["Twitter.ConsumerKey"];
                        string consumerSecret = ConfigurationManager.AppSettings["Twitter.ConsumerSecret"];
                        string accessToken = ConfigurationManager.AppSettings["Twitter.AccessToken"];
                        string accessSecret = ConfigurationManager.AppSettings["Twitter.AccessSecret"];

                        // Obtain keys by registering your app on https://dev.twitter.com/apps or https://apps.twitter.com/
                        var service = new TwitterService(consumerKey, consumerSecret);
                        service.AuthenticateWith(accessToken, accessSecret);
                        SendTweetOptions x = new SendTweetOptions();
                        x.Status = string.Format("Gas leak detected from Smart heater! (Auto-generated messsage from Azure Stream Analytics)");
                        service.SendTweet(x);

                        lastTweet = DateTime.Now;
                    }

                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("Error in ProssEventsAsync -- {0}\n", ex.Message);
                }
            }

            await context.CheckpointAsync();
        }
        private SensorEvent DeserializeEventData(string eventDataString)
        {
            return JsonConvert.DeserializeObject<SensorEvent>(eventDataString);
        }

    }
}
