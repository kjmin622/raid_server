using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.IO.Compression;
using System.Text.Json;
using cresent_overflow_server.packet;

namespace cresent_overflow_server
{
    class Program 
    {

        static void ThreadMain(int port, string difficulty) 
        {

            TcpListener listener = new TcpListener(IPAddress.Any, port);
            TcpClient[] clients = new TcpClient[Constant.MAXIMUM];
            NetworkStream[] streams = new NetworkStream[Constant.MAXIMUM];
            ClientInfo[] clients_info = new ClientInfo[Constant.MAXIMUM];
            DateTime server_start_time = DateTime.Now;

            listener.Start();

            
            WaitRoom waitroom = new WaitRoom(port, listener, clients, streams, clients_info, server_start_time);
            waitroom.Start();
            // 5분동안 유저 진입
            // 풀꽉 시, 대기 / 빈 자리 있으면 들어가기
            // 3초마다 모든 유저에게 현재 남은 시간(초)과 현재 들어온 유저 데이터 쏴줌

            Raid raid = new Raid(port, listener, clients, streams, clients_info, server_start_time);
            raid.Start();

            Console.WriteLine("?");
            Thread.Sleep(10000000);
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
            //thread_hard_server1.Start();


            // 종료
            thread_easy_server1.Join();
            //thread_hard_server1.Join();
        }
    }    
}