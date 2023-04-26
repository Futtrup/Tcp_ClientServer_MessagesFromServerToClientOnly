using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Client
    {
        // Public
        public event EventHandler<string> LogEvent;
        public bool Run
        {
            get { lock (_run_LckObj) return _run; }
            set { lock (_run_LckObj) _run = value; }
        }

        // Private
        private bool _run = false;
        private object _run_LckObj = new object();

        private TcpClient _client;
        private int _port;
        private string _ip;

        private DateTime _lastReceivedDataFromServer;
        private int _reconnectWaitTime_ms = 10 * 1000;

        public Client(string serverIp = "127.0.0.1", int serverPort = 8000)
        {
            _ip = serverIp;
            _port = serverPort;
        }

        public void Start_Client()
        {
            Run = true;
            ConnectToServerAndReceiveData();
        }

        public void Stop_Client()
        {
            try
            {
                Run = false;

                if (_client != null)
                    _client.Dispose();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void ConnectToServerAndReceiveData()
        {
            while (Run)
            {
                Log($"Connecting to server {_ip}:{_port}");
                try
                {
                    _client = new TcpClient(_ip, _port);
                    Log($"Connected to server:{_client.Client.RemoteEndPoint}");
                    ReceiveData_FromServer(_client);
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                }

                Log($"Could not connect to server {_ip}:{_port}. Try connect to server again in ms:{_reconnectWaitTime_ms}");
                Thread.Sleep(_reconnectWaitTime_ms);
            }
        }

        private void ReceiveData_FromServer(TcpClient client)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            try
            {
                Thread thread_HeartBeat = new Thread(() => HeartBeat(client, 5000, ct));
                thread_HeartBeat.IsBackground = true;
                thread_HeartBeat.Start();

                NetworkStream ns = client.GetStream();
                StreamReader sr = new StreamReader(ns);
                string msgFromServer;
                bool closed = false;

                while (!closed)
                {
                    if (ct.IsCancellationRequested)
                        ct.ThrowIfCancellationRequested();

                    try
                    {
                        msgFromServer = sr.ReadLine();
                        Log(msgFromServer);
                    }
                    catch (EndOfStreamException)
                    {
                        closed = true;
                        cts.Cancel();
                        thread_HeartBeat.Abort();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }


        private void HeartBeat(TcpClient client, int heartBeat_Interval_ms, CancellationToken ct)
        {
            try
            {
                StreamWriter sr = new StreamWriter(client.GetStream());

                while (true)
                {
                    if (ct.IsCancellationRequested)
                        ct.ThrowIfCancellationRequested();

                    sr.WriteLine("<Heartbeat/>");
                    sr.Flush();
                    Thread.Sleep(heartBeat_Interval_ms);
                }
            }
            catch (OperationCanceledException oex)
            {
                Log(oex.Message);
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        protected virtual void Log(string msg) =>
            LogEvent?.Invoke(this, msg);
    }
}
