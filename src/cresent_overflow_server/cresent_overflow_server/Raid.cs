using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace cresent_overflow_server
{
    public class Raid
    {
        private TcpListener listener;
        private TcpClient[] tcs;
        private NetworkStream[] streams;
        private struct playerInfo 
        {
            public string ID;
            public string characterID;
            public int HP;
        }
        public Raid()
        {
            tcs = new TcpClient[30];
            listener = new TcpListener(IPAddress.Any, 7000);
            listener.Start();
        }
        public void joinGame() 
        {
            int idx = 0;
            int tmpidx = 0;
            int nbytes;
            for (var i = 0; i < 30; i++)
            {
                tcs[i] = null;
            }
            while (true) 
            {
                if (idx != -1) 
                {
                    try
                    {
                        tcs[idx] = listener.AcceptTcpClient();
                    }
                    catch
                    {
                        Console.WriteLine("client listen error");
                    }
                    try
                    {
                        streams[idx] = tcs[idx].GetStream();
                        nbytes = 0;
                        byte[] buff = new byte[1024];
                        while ((nbytes = streams[idx].Read(buff, 0, buff.Length)) > 0) 
                        { 
                        }
                    }
                    catch
                    {
                        Console.WriteLine("client info get error");
                    }
                    tmpidx = idx;
                    for (var i = 0; i < 30; i++)
                    {
                        if (tcs[idx] == null) idx = i;
                    }
                    if (tmpidx == idx) idx = -1;

                }
                
            }
            
        }
    }
}
