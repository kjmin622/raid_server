using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace cresent_overflow_server.packet
{
    [DataContract]
    public class EnemyHealInfo
    {
        [DataMember]
        public string enemy_id { get; set; }
        [DataMember]
        public string target_id { get; set; }
        [DataMember]
        public int heal { get; set; }
    }
}
