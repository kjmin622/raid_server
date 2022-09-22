using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace testclient
{
    [DataContract]
    public class ClientInfo
    {
        [DataMember]
        public string client_id { get; set; }
        [DataMember]
        public string character_id { get; set; }
        [DataMember]
        public int hp { get; set; }
    }
}
