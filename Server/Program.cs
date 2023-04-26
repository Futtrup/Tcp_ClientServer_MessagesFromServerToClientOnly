using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        private static int _port = 8006;
        private static Server _server;
        private static Thread _server_ListenerThread;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello from server...");

            _server = new Server(_port);
            _server.LogEvent += (s, e) => Console.WriteLine(e);

            _server_ListenerThread = new Thread(_server.Start_ServerListener);
            _server_ListenerThread.IsBackground = true;
            _server_ListenerThread.Start();

            string msgToClients;
            while (true)
            {
                msgToClients = DateTime.Now.ToString();
                Console.WriteLine($"Sending message to clients:{msgToClients}");
                _server.Send_MessageToClients(msgToClients);
                Thread.Sleep(5 * 1000);
            }
        }
    }
}
