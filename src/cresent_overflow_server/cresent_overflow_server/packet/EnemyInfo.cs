using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace cresent_overflow_server.packet
{
    [DataContract]
    public class EnemyInfo
    {
        [DataMember]
        public string enemy_id { get; set; }
        [DataMember]
        public string enemy_sid { get; set; }
        [DataMember]
        public int hp { get; set; }
        [DataMember]
        public string[] status_ailment_id { get; set; }
        [DataMember]
        public string[] status_ailment_time { get; set; }
    }
}
