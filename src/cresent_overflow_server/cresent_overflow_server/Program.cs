using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.IO.Compression;
using System.Text.Json;

namespace cresent_overflow_server
{
    class Program 
    {
        static void ThreadMain(int port, string difficulty) 
        {
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            TcpClient[] clients = new TcpClient[30];
            NetworkStream[] streams = new NetworkStream[30];
            ClientInfo[] clients_info = new ClientInfo[30];
            BinaryFormatter binary_fomatter = new BinaryFormatter();
            DateTime server_start_time = DateTime.Now;

            listener.Start();

            // 5분동안 소켓 연결 받기
            int client_cnt = 0;
            while ((DateTime.Now - server_start_time).TotalMinutes < 5)
            {
                Thread.Sleep(500);
                // 들어올 자리가 있을 때 연결 요청 시 받아주기
                if(client_cnt != 30 && listener.Pending())
                {
                    try
                    {
                        Console.WriteLine("try Connect");
                        clients[client_cnt] = listener.AcceptTcpClient();
                        Console.WriteLine("- client accept success");
                        streams[client_cnt] = clients[client_cnt].GetStream();
                        streams[client_cnt].ReadTimeout = 5000;
                        streams[client_cnt].WriteTimeout = 5000;
                        Console.WriteLine("- get stream success");
                        
                        
                        byte[] bytes = new byte[1024];
                        int length = streams[client_cnt].Read(bytes, 0, bytes.Length);
                        Array.Resize(ref bytes, length);
                        string data = Encoding.Default.GetString(bytes);
                        clients_info[client_cnt] = JsonSerializer.Deserialize<ClientInfo>(data);
                        Console.WriteLine("- user info read success");
                        Console.WriteLine("- Connect Success: ");

                    }
                    catch
                    {
                        Console.WriteLine("client connect error");
                        try
                        {
                            clients[client_cnt].Close();
                            Console.WriteLine("- client socket unconnect success");
                        }
                        catch
                        {
                            Console.WriteLine("- client socket unconnect fail");
                        }
                        try
                        {
                            streams[client_cnt].Close();
                            Console.WriteLine("- stream unconnect success");
                        }
                        catch
                        {
                            Console.WriteLine("- stream unconnect fail");
                        }
                        clients[client_cnt] = null;
                        streams[client_cnt] = null;
                    }
                    for (int i = 0; i < 30; i++)
                    {
                        if (clients[i] == null)
                        {
                            client_cnt = i;
                            break;
                        }
                    }

                }


                // 클라이언트에 시작까지 남은 시간(초) 뿌려주기 + 연결 유지 중인지 확인
                byte[] nowtime = new byte[20];
                for (int i = 0; i < 30; i++) 
                {
                    if (clients[i] != null)
                    {
                        try
                        {
                            nowtime = Encoding.Default.GetBytes(Convert.ToInt32((DateTime.Now - server_start_time).TotalSeconds).ToString());
                            streams[i].Write(nowtime, 0, nowtime.Length);
                            Console.WriteLine(i+" connect checked");
                        }
                        catch
                        {
                            Console.WriteLine("client unconnected (>5000ms)");
                            try
                            {
                                clients[i].Close();
                                streams[i].Close();
                            }
                            catch
                            {
                                Console.WriteLine("unconnected fail");
                            }
                            clients[i] = null;
                            streams[i] = null;
                        }
                    }
                }
            }
        }

        static void Main(string[] args) 
        {
            int[] port_set = Enumerable.Range(7000, 7010).ToArray();

            Thread thread_easy_server1;
            Thread thread_hard_server1;
            
            thread_easy_server1 = new Thread(new ThreadStart(
                () => ThreadMain(port_set[0], "easy")
                ));
            thread_hard_server1 = new Thread(new ThreadStart(
                () => ThreadMain(port_set[1], "hard")
                ));


            thread_easy_server1.Start();
            thread_hard_server1.Start();


            // 종료
            thread_easy_server1.Join();
            thread_hard_server1.Join();
        }
    }    
}