using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using cresent_overflow_server.packet;

namespace cresent_overflow_server
{
    class WaitRoom
    {
        private int port;
        private TcpListener listener;
        private TcpClient[] clients;
        private NetworkStream[] streams;
        private ClientInfo[] clients_info;
        private DateTime server_start_time;
        private int client_cnt;

        public WaitRoom(int port, TcpListener listener, TcpClient[] clients, NetworkStream[] streams, ClientInfo[] clients_info, DateTime server_start_time) 
        {
            this.port = port;
            this.listener = listener;
            this.clients = clients;
            this.streams = streams;
            this.clients_info = clients_info;
            this.server_start_time = server_start_time;
            this.client_cnt = 0;
        }

        public void Start()
        {
            while ((Utility.Today() - server_start_time).TotalMinutes < Constant.WAITMINUTE)
            {
                
                Thread.Sleep(1000);
                Funcs.Print(Convert.ToInt32((Utility.Today() - server_start_time).TotalSeconds), port);
                if (client_cnt != Constant.MAXIMUM && listener.Pending())
                    AcceptPlayer();

                SendInfoForPlayers();
                SetClientCnt();
            }
            SendEndWait();
        }

        private void AcceptPlayer()
        {
            try
            {
                Funcs.Print("try Connect", port);
                clients[client_cnt] = listener.AcceptTcpClient();
                Funcs.Print("- client accept success", port);
                streams[client_cnt] = clients[client_cnt].GetStream();
                streams[client_cnt].ReadTimeout = 5000;
                streams[client_cnt].WriteTimeout = 5000;
                Funcs.Print("- get stream success", port);


                string data = Funcs.PacketToString(streams[client_cnt], 1024);
                ClientInfo tmpinfo = JsonSerializer.Deserialize<ClientInfo>(data);
                foreach(ClientInfo info in clients_info)
                { 
                    if(info != null)
                    {
                        if(tmpinfo.client_id == info.client_id)
                        { 
                            Funcs.Print("already join player",port);
                            throw(new IOException());
                        }
                    }
                }
                clients_info[client_cnt] = tmpinfo;
                Funcs.Print($"{client_cnt}: ({clients_info[client_cnt].client_id},{clients_info[client_cnt].character_id},{clients_info[client_cnt].hp},{clients_info[client_cnt].phsysical_defense},{clients_info[client_cnt].magic_defense})",port);
                Funcs.Print("- user info read success", port);

                byte[] accept_masage = Funcs.StringToByteArray("AcceptComplete");
                streams[client_cnt].Write(accept_masage, 0, accept_masage.Length);
                Funcs.Print("- Connect Success: ", port);

            }
            catch
            {
                Funcs.Print("client connect error", port);
                clients[client_cnt].Close();
                streams[client_cnt].Close();
                clients_info[client_cnt] = null;
                clients[client_cnt] = null;
                streams[client_cnt] = null;
            }
        }
        private void SetClientCnt()
        {
            for (client_cnt = 0; client_cnt < Constant.MAXIMUM; client_cnt++)
            {
                if (clients[client_cnt] == null)
                {
                    break;
                }
            }
        }

        private void SendInfoForPlayers()
        {
            string senddata_str = "";
            byte[] senddata_byte;
            for (int i = 0; i < Constant.MAXIMUM; i++)
            {
                if (clients[i] != null)
                {
                    streams[i].Flush();
                    try
                    {
                        senddata_str += "$" + Convert.ToInt32((Utility.Today() - server_start_time).TotalSeconds).ToString() + "&";
                        for (int j = 0; j < Constant.MAXIMUM; j++)
                        {
                            if (clients[j] != null)
                            {
                                senddata_str += JsonSerializer.Serialize(clients_info[j]) + "&";

                            }
                        }
                        senddata_byte = Encoding.UTF8.GetBytes(senddata_str);
                        streams[i].Write(senddata_byte, 0, senddata_byte.Length);
                        Funcs.Print(i + " connect checked", port);

                    }
                    catch
                    {
                        Funcs.Print("client unconnected (>5000ms)", port);
                        try
                        {
                            clients[i].Close();
                            streams[i].Close();
                        }
                        catch
                        {
                            Funcs.Print("unconnected fail", port);
                        }
                        clients[i] = null;
                        streams[i] = null;
                    }
                }
            }
        }

        private void SendEndWait()
        {
            for (int i = 0; i < Constant.MAXIMUM; i++) 
            {
                if (clients[i] != null)
                {
                    byte[] senddata = Funcs.StringToByteArray("$waitend");
                    Funcs.SendByteArray(streams[i],senddata);
                }
            }
        }
    }
}
