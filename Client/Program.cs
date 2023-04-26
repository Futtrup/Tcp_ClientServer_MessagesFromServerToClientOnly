using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        private static string _ip = "127.0.0.1";
        private static int _port = 8006;
        private static Client _client;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello from client...");

            _client = new Client(_ip, _port);
            _client.LogEvent += (s, e) => Console.WriteLine(e);
            _client.LastReceivedData_Timeout += (s, e) => Console.WriteLine("Client timeout event occured");
            _client.Start_Client();

            Console.ReadLine();
        }
    }
}
