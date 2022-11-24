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
        public string enemy_sid { get; set; } // 클라이언트가 가지는 적 정보 공통 id
        [DataMember]
        public int max_hp { get; set; }
        [DataMember]
        public int hp { get; set; }
        [DataMember]
        public List<string> status_ailment_id { get; set; }
        [DataMember]
        public List<string> status_ailment_time { get; set; }
    }
}
