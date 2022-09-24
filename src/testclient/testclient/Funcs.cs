using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace client
{
    public class Funcs
    {
        public static void Print(object str, int port)
        {
            string s = str.ToString();
            Console.WriteLine(port + ": " + s);
        }

        public static string PacketToString(NetworkStream stream, int bytesize)
        {
            byte[] bytes = new byte[bytesize];
            int length = stream.Read(bytes, 0, bytes.Length);
            Array.Resize(ref bytes, length);
            string data = Encoding.Default.GetString(bytes);
            return data;
        }

        public static byte[] StringToByteArray(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public static string DataToString<T>(T data)
        {
            string sdata = JsonSerializer.Serialize(data);
            return sdata;
        }

        public static void StringToData<T>(T data, string str)
        { 
            data = JsonSerializer.Deserialize<T>(str);
        }

        public static void SendByteArray(NetworkStream stream, byte[] buff)
        {
            try
            {
                stream.Write(buff, 0, buff.Length);
            }
            catch { }
        }
    }
}
