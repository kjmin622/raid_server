using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace cresent_overflow_server.packet
{
    [DataContract]
    public class EnemyUseSkillInfo
    {
        [DataMember]
        public string enemy_id { get; set; }
        [DataMember]
        public string skill_id { get; set; }
        [DataMember]
        public bool to_player { get; set; } // 플레이어에게 사용했는가 ?
        [DataMember]
        public string target_id { get; set; }
    }
}
