using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using client;

namespace client
{
    public class Overflow
    {
        public ClientInfo clientinfo;
        public TcpClient client;
        public NetworkStream stream;
        public string difficulty;
        private string server_ip;
        private int port;
      
        public Overflow(string server_ip, int port, string difficulty, string client_id, string character_id, int hp)
        {
            
            
            this.server_ip = server_ip;
            this.port = port;

        }

        public async Task<bool> Join()
        {
            try
            {
                await Task.Run(() =>
                {
                    this.client = new TcpClient(server_ip, port);
                    this.stream = client.GetStream();
                });

                this.clientinfo = new ClientInfo { client_id = "TestclientID", character_id = "TestcharacterID", hp = 300 };
                byte[] buff = Funcs.StringToByteArray(Funcs.DataToString(clientinfo));

                await Task.Run(() =>
                {
                    Funcs.SendByteArray(this.stream, buff);
                    Funcs.PacketToString(this.stream, 100);
                });

                return true;
            }
            catch
            {
                return false;
            }
            
        }

    }
}
