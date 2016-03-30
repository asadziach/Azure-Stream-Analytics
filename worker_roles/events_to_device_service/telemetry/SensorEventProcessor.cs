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
                    String receiverid;
                    string datatosend;
                    //TODO It can be made better by mataining runtime registry of devices and controllers.
                    if (jsonString.Contains("\"deviceid\"") || jsonString.Contains("\"DeviceId\"") ) // Telemetry 
                    {
                        receiverid = "UWA1";
                        SensorEvent newSensorEvent = this.DeserializeEventData(jsonString);


                        Trace.TraceInformation(string.Format("-->Serialized Data: '{0}', '{1}', '{2}', '{3}'",
                            newSensorEvent.deviceid, newSensorEvent.currenttemp, newSensorEvent.gassense, newSensorEvent.flamesense));

                        datatosend = jsonString;
                    }
                    else { // command
                        receiverid = "Heater1";
                        HeaterCommand heatercommand = JsonConvert.DeserializeObject<HeaterCommand>(jsonString);
                        datatosend = "{\"Name\":\""+ heatercommand.command + "\",\"Parameters\":{";
                        if (heatercommand.parameterName != null)
                        {
                            datatosend += "\"" + heatercommand.parameterName + "\":" + heatercommand.parameter + "";
                        }
                        datatosend += "}}";
                    }
                    /* Send command to IoT device. */
                    

                    Trace.TraceInformation("Sending jason to: '{0}'", receiverid);
                    Trace.TraceInformation("Data: '{0}'", datatosend);
                    await WorkerRole.iotHubServiceClient.SendAsync(receiverid, new Microsoft.Azure.Devices.Message(Encoding.UTF8.GetBytes(datatosend)));
                    

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
