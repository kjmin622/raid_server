﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace cresent_overflow_server.packet
{
    [DataContract]
    public class EnemyAttackInfo
    {
        [DataMember]
        public string enemy_id { get; set; }
        [DataMember]
        public string client_id { get; set; }
    }
}
