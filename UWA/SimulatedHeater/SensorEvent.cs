using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SimHeater
{
    [DataContract]
    class SensorEvent
    {
        [DataMember]
        public string deviceid { get; set; }

        [DataMember]
        public int currenttemp { get; set; }

        [DataMember]
        public int gassense { get; set; }

        [DataMember]
        public int flamesense { get; set; }

        [DataMember]
        public int humidty { get; set; }
    }
}
