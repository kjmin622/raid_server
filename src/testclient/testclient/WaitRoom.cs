using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace client
{
    class WaitRoom
    {
        private Overflow overflow;
        public int seconds_remaining;
        public ClientInfo[] clients_info;

        public WaitRoom(Overflow overflow)
        { 
            this.overflow = overflow;
            clients_info = new ClientInfo[30];
        }

        public async Task Start()
        {
            while (true)
            {
                byte[] buffer = new byte[10240];
                string getdata = Funcs.PacketToString(overflow.stream, 10240);
                string[] alldata = getdata.Split('$');
                if (alldata.Length>0 && alldata[alldata.Length - 1] == "waitend")
                {
                    break;
                }
                string[] datalist = alldata[alldata.Length - 1].Split('&');
                Array.Resize(ref datalist, datalist.Length - 1);
                this.seconds_remaining = Convert.ToInt32(datalist[0]);
                for (int i=0; i<30; i++)
                {
                    if (i + 1 >= datalist.Length)
                    {
                        clients_info[i] = null;
                    }
                    else
                    {
                        clients_info[i] = JsonSerializer.Deserialize<ClientInfo>(datalist[i + 1]);
                    }
                }
            }
        }
    }
}
