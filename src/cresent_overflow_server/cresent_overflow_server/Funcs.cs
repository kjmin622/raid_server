using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace cresent_overflow_server
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

        public static void SendByteArray(NetworkStream stream, byte[] buff)
        {
            try
            {
                stream.Write(buff, 0, buff.Length);
            }
            catch { }
        }

        public static Dictionary<string, List<string>> TranslateAString(string str)
        {
            Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();

            string[] class_and_json_together = str.Split('$');
            Array.Resize(ref class_and_json_together,class_and_json_together.Length-1);
            foreach(string cj in class_and_json_together)
            {
                string[] devide_class_json = cj.Split('&');
                if (dict.ContainsKey(devide_class_json[0])) 
                {
                    dict[devide_class_json[0]].Add(devide_class_json[1]);
                }
                else
                {
                    dict.Add(devide_class_json[0], new List<string>());
                    dict[devide_class_json[0]].Add(devide_class_json[1]);
                }
                
            }
            return dict;
        }
    }
}
