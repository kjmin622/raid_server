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
        public bool is_player { get; set; }
        [DataMember]
        public string target_id { get; set; }
    }
}
