using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Server
    {
        // Public
        public event EventHandler<string> LogEvent;

        public TcpClient[] Get_Clients()
        {
            lock (_lock) return _clients.ToArray();
        }
        public int Get_ClientCount()
        {
            lock (_lock) return _clients.Count;
        }
        public void Remove_Client(TcpClient client)
        {
            lock (_lock) _clients.Remove(client);
            Log($"Client disconnected: {client.Client.RemoteEndPoint}");
            Log_ClientsConnected();
        }

        public void Add_Client(TcpClient client)
        {
            lock (_lock) _clients.Add(client);
            Log($"Client connected: {client.Client.RemoteEndPoint}");
            Log_ClientsConnected();
        }

        // Private
        private readonly object _lock = new object();
        private List<TcpClient> _clients;
        private TcpListener _listener;
        private int _port;
        private bool _run = false;


        public Server(int port = 8000)
        {
            _port = port;
            _clients = new List<TcpClient>();
        }

        public void Start_ServerListener()
        {
            lock (_lock) _run = true;
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            Log($"Listening for clients on port {_port}");

            while (_run)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Add_Client(client);
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                }
            }

            Log($"Stopped listening for clients");
        }

        public void Stop_ServerListener()
        {
            try
            {
                lock (_lock) _run = false;
                _listener.Stop();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        public int Send_MessageToClients(string msg)
        {
            int res = 0;
            try
            {
                if (Get_ClientCount() == 0)
                    return res;


                foreach (var c in Get_Clients())
                {
                    try
                    {
                        StreamWriter sw = new StreamWriter(c.GetStream());
                        sw.WriteLine(msg);
                        sw.Flush();
                        res++;
                    }
                    catch
                    {
                        Remove_Client(c);
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            return res;
        }


        private void Log_ClientsConnected()
        {
            Log($"Clients connected: {Get_ClientCount()}");
            foreach (var c in Get_Clients())
                Log($"{c.Client.RemoteEndPoint}");
        }

        protected virtual void Log(string message) =>
            LogEvent?.Invoke(this, message);
    }
}
