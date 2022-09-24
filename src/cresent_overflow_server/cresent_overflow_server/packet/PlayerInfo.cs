﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace cresent_overflow_server.packet
{
    [DataContract]
    public class PlayerInfo
    {
        [DataMember]
        public string client_id { get; set; }
        [DataMember]
        public string character_id { get; set; }
        [DataMember]
        public int hp { get; set; }
        [DataMember]
        public string[] status_ailment_id { get; set; }
        [DataMember]
        public string[] status_ailment_time { get; set; }
        [DataMember]
        public int total_deal { get; set; }

        [DataMember]
        public int total_heal { get; set; }
    }
}