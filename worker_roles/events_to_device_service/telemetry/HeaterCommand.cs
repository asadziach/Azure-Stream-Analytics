using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace events_forwarding
{
    [DataContract]
    class HeaterCommand
    {
        [DataMember]
        public string command { get; set; }

        [DataMember]
        public string parameterName { get; set; }

        [DataMember]
        public int parameter { get; set; }
    }
}
