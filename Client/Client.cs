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
        public event EventHandler LastReceivedData_Timeout;
        public bool Run
        {
            get { lock (_run_LckObj) return _run; }
            set { lock (_run_LckObj) _run = value; }
        }

        public DateTime? LastReceivedData
        {
            get { lock (_lastReceivedData_LckObj) return _lastReceivedData; }
            set { lock (_lastReceivedData_LckObj) _lastReceivedData = value; }
        }

        public CancellationTokenSource TokenSource
        {
            get { lock (_tokenSource_LckObj) return _tokenSource; }
            set { lock (_tokenSource_LckObj) _tokenSource = value; }
        }

        public TcpClient ClientHandle
        {
            get { lock (_client_LckObj) return _clientHandle; }
            set { lock (_client_LckObj) _clientHandle = value; }
        }

        // Private
        private CancellationTokenSource _tokenSource = null;
        private object _tokenSource_LckObj = new object();

        private bool _run = false;
        private object _run_LckObj = new object();

        private TcpClient _clientHandle = null;
        private object _client_LckObj = new object();
        private int _port;
        private string _ip;

        private DateTime? _lastReceivedData = null;
        private object _lastReceivedData_LckObj = new object();
        private int _connectionRetry_ms = 10 * 1000;
        private int _heartbeat_ms = 5 * 1000;
        private double _timeout_ms = 10 * 1000;

        public Client(string serverIp = "127.0.0.1", int serverPort = 8000)
        {
            _ip = serverIp;
            _port = serverPort;
        }

        public void Start_Client()
        {
            Run = true;

            while (Run)
            {
                try
                {
                    Log("Starting client...");
                    ConnectToServerAndReceiveData();
                }
                catch (Exception ex)
                {
                    Log(ex.Message);
                }

                Log($"No connection to server, try again in ms:{_connectionRetry_ms}");
                Thread.Sleep(_connectionRetry_ms);
            }
        }

        public void Stop_Client()
        {
            try
            {
                Run = false;

                if (ClientHandle != null)
                    ClientHandle.Dispose();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void ConnectToServerAndReceiveData()
        {
            Log($"Connecting to server {_ip}:{_port}");
            try
            {
                TokenSource = new CancellationTokenSource();
                CancellationToken ct = TokenSource.Token;

                ClientHandle = new TcpClient(_ip, _port);
                if (ClientHandle == null)
                    throw new Exception("No connection to server");
                else
                    Log($"Connected to server:{ClientHandle.Client.RemoteEndPoint}");

                NetworkStream ns = ClientHandle.GetStream();
                Task t_ReceiveData = Task.Run(() => ReceiveData(ns, ct), ct);
                Start_HeartBeat(ns, _heartbeat_ms);
            }
            catch (Exception ex)
            {
                TokenSource.Cancel();
                TokenSource.Dispose();
                if (ClientHandle != null) ClientHandle.Dispose();
                Log(ex.Message);
            }
        }

        private void ReceiveData(NetworkStream ns, CancellationToken ct)
        {
            try
            {
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
                        LastReceivedData = DateTime.Now;
                        Log(msgFromServer);
                    }
                    catch (EndOfStreamException)
                    {
                        closed = true;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }


        private void Start_HeartBeat(NetworkStream ns, int heartBeat_Interval_ms)
        {
            LastReceivedData = DateTime.Now;
            StreamWriter sr = new StreamWriter(ns);

            bool runHeartBeat = true;
            while (runHeartBeat)
            {
                try
                {
                    double ms_sincelastData = (DateTime.Now - (DateTime)LastReceivedData).TotalMilliseconds;
                    if (ms_sincelastData > _timeout_ms)
                    {
                        On_LastReceivedData_Timeout();
                        throw new TimeoutException($"Timeout exception. Milliseconds since last data received:{ms_sincelastData}");
                    }

                    sr.WriteLine("<Heartbeat/>");
                    sr.Flush();

                    Thread.Sleep(heartBeat_Interval_ms);
                }
                catch (Exception ex)
                {
                    runHeartBeat = false;
                    Log(ex.Message);
                }
            }
            throw new Exception("Heartbeat/timeout exception");
        }

        protected virtual void On_LastReceivedData_Timeout() =>
            LastReceivedData_Timeout?.Invoke(this, new EventArgs());

        protected virtual void Log(string msg) =>
            LogEvent?.Invoke(this, msg);
    }
}
