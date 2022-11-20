using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace client.packet
{
    [DataContract]
    public class PlayerDamageInfo
    {
        [DataMember]
        public string client_id { get; set; }
        [DataMember]
        public string enemy_id { get; set; }
        [DataMember]
        public int damage { get; set; }
    }
}
