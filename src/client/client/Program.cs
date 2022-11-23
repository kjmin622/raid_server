using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace client
{
    class Program
    {
        private static void ThreadWaitRoom(NetworkStream stream)
        {
            string sendTestClientData = "{\"client_id\":\"testclient\",\"character_id\":\"testcharacter\",\"hp\":1000}";
            byte[] sendBuffer = Encoding.UTF8.GetBytes(sendTestClientData);
            stream.Write(sendBuffer, 0, sendBuffer.Length);

            while (true)
            {
                byte[] recvBuffer = new byte[1024];
                int length = stream.Read(recvBuffer, 0, recvBuffer.Length);
                Array.Resize(ref recvBuffer, length);
                string strRecvMsg = Encoding.Default.GetString(recvBuffer);

                string[] splitStr = strRecvMsg.Split('$');
                foreach(string str in splitStr)
                {
                    foreach (string s in str.Split('&'))
                    {
                        Console.Write(s + " ");
                    }
                }Console.WriteLine();

                if (splitStr[splitStr.Length-1] == "waitend") return;
            }
        }

        private static void ThreadRaidRoom(NetworkStream stream)
        {
            while (true)
            {
                byte[] recvBuffer = new byte[10240];
                int length = stream.Read(recvBuffer, 0, recvBuffer.Length);
                Array.Resize(ref recvBuffer, length);
                string strRecvMsg = Encoding.Default.GetString(recvBuffer);

                string[] splitStr = strRecvMsg.Split('$');
                foreach (string str in splitStr)
                {
                    foreach (string s in str.Split('&'))
                    {
                        Console.Write(s + " ");
                    }
                }
                Console.WriteLine();
            }
        }

        static void Main(string[] args)
        {
            string strRecvMsg;
            string strSendMsg;

            TcpClient sockClient = new TcpClient("127.0.0.1", 7000);
            NetworkStream stream = sockClient.GetStream();

            Thread threadWaitRoom = new Thread(new ThreadStart(() => ThreadWaitRoom(stream)));
            Thread threadRaidRoom = new Thread(new ThreadStart(() => ThreadRaidRoom(stream)));
            
            threadWaitRoom.Start();

            threadWaitRoom.Join();

            threadRaidRoom.Start();
            threadRaidRoom.Join();


            stream.Close();
            sockClient.Close();
        }   
    }
  
}