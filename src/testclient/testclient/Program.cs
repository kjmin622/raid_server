using System;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using client;

namespace client
{
    class Program
    {

        static async Task Main(string[] args)
        {
            Overflow main_server = new Overflow(Constant.SERVER_IP, Constant.EASYROOM1_PORT, "easy", "testclientid", "testchracter_id", 300);
            Task serverjoin = main_server.Join();
            WaitRoom wait_server = new WaitRoom(main_server);
            await serverjoin;
            Task wait_server_join = wait_server.Start();
            Console.WriteLine("12");
            Console.WriteLine("12");
            await wait_server_join;
        }
    }
}