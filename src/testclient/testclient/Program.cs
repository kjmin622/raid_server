using testclient;
using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace testclient
{
    class Program
    {
        static void Main(string[] args)
        {
            BinaryFormatter binary_formatter = new BinaryFormatter();
            MemoryStream memory_stream = new MemoryStream();
            ClientInfo clientinfo = new ClientInfo { client_id = "TestclientID", character_id = "TestcharacterID", hp = 300 };
            // (1) IP 주소와 포트를 지정하고 TCP 연결 
            TcpClient tc = new TcpClient("127.0.0.1", 7000);

            // (2) NetworkStream을 얻어옴 
            NetworkStream stream = tc.GetStream();

            byte[] buff = new byte[1024];
            string data = JsonSerializer.Serialize(clientinfo);
            buff = Encoding.UTF8.GetBytes(data);
            // (3) 스트림에 바이트 데이타 전송
            stream.Write(buff, 0, buff.Length);

            byte[] buff2 = new byte[20];
            Thread.Sleep(2000);
            while (true) 
            {
                
                while ((stream.Read(buff2, 0, buff2.Length)) > 0) 
                {
                    Console.WriteLine(Encoding.Default.GetString(buff2));
                }
            }
            // (5) 스트림과 TcpClient 객체 닫기
            memory_stream.Close();
            stream.Close();
            tc.Close();
        }
    }
}