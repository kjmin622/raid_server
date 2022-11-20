using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace client.packet
{
	[DataContract]
	public class InflictStatusAilmentInfo
	{
		[DataMember]
		public string target_id { get; set; }
		[DataMember]
		public bool is_player { get; set; }
		[DataMember]
		public string status_ailment_id { get; set; }
		[DataMember]
		public string status_ailment_time { get; set; }
	}
}
