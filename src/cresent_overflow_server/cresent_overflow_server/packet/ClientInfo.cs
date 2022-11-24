using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace cresent_overflow_server.packet
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
        [DataMember]
        public int phsysical_defense { get; set; }
        [DataMember]
        public int magic_defense { get; set; }
    }
}
