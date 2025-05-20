using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    /// <summary>TCP comm.</summary>
    /// <see cref="IComm"/>
    internal class TcpComm : IComm
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("TCP");
        Config _config = new();// = null;
        TcpClient _client = new();// = null;
        string _host = "???";
        int _port = 0;
        #endregion



        #region IComm implementation
        public Stream? AltStream { get; set; } = null;

        //public (OpStatus stat, string msg) Init(Config config)
        public TcpComm(Config config)
        {
            OpStatus stat;
            string msg = "";

            try
            {
                _config = config;
                // Just in case.
                //Dispose();

                // Parse the args. "127.0.0.1 59120"
                var parts = _config.Args;
                stat = parts.Count == 2 ? OpStatus.Success : OpStatus.Error;

                _host = parts[0];
                _port = int.Parse(parts[1]);

                IPEndPoint ipEndPoint = new(IPAddress.Parse(_host), _port);

                //TcpClient()
                //This constructor creates a new TcpClient and allows the underlying service provider to assign the most
                //appropriate local IP address and port number. You must first call the Connect method before sending
                //and receiving data.

                //TcpClient(IPEndPoint)
                //This constructor creates a new TcpClient and binds it to the IPEndPoint specified by the localEP parameter.
                //Before you call this constructor, you must create an IPEndPoint using the IP address and port number from which
                //you intend to send and receive data. You do not need to specify a local IP address and port number
                //before connecting and communicating. If you create a TcpClient using any other constructor, the underlying
                //service provider will assign the most appropriate local IP address and port number.
                //You must call the Connect method before sending and receiving data.

                //TcpClient(String, Int32)
                //This constructor creates a new TcpClient and makes a synchronous connection attempt to the provided
                //host name and port number. The underlying service provider will assign the most appropriate local
                //IP address and port number. TcpClient will block until it either connects or fails. This constructor
                //allows you to initialize, resolve the DNS host name, and connect in one convenient step.


                //_client = new TcpClient(ipEndPoint)
                //_client = new TcpClient(_host, _port)
                _client = new TcpClient()
                {
                    // Set some properties.
                    SendTimeout = _config.ResponseTime,
                    ReceiveTimeout = _config.ResponseTime,
                    SendBufferSize = _config.BufferSize,
                    ReceiveBufferSize = _config.BufferSize
                };
            }
            catch (Exception e)
            {
                msg = $"Invalid comm args - {e.Message}";
                stat = OpStatus.Error;
                throw new ArgumentException(msg);
            }

            //return (stat, msg);
        }

        // public (OpStatus stat, string msg) Init(Config config)
        // {
        //     _config = config;
        //     OpStatus stat;
        //     string msg = "";
        //     // Just in case.
        //     Dispose();

        //     try
        //     {
        //         // Parse the args. "127.0.0.1 59120"
        //         var parts = _config.Args;
        //         stat = parts.Count == 2 ? OpStatus.Success : OpStatus.Error;

        //         _host = parts[0];
        //         _port = int.Parse(parts[11]);

        //         IPEndPoint ipEndPoint = new(IPAddress.Parse(_host), _port);

        //         //TcpClient()
        //         //This constructor creates a new TcpClient and allows the underlying service provider to assign the most
        //         //appropriate local IP address and port number. You must first call the Connect method before sending
        //         //and receiving data.

        //         //TcpClient(IPEndPoint)
        //         //This constructor creates a new TcpClient and binds it to the IPEndPoint specified by the localEP parameter.
        //         //Before you call this constructor, you must create an IPEndPoint using the IP address and port number from which
        //         //you intend to send and receive data. You do not need to specify a local IP address and port number
        //         //before connecting and communicating. If you create a TcpClient using any other constructor, the underlying
        //         //service provider will assign the most appropriate local IP address and port number.
        //         //You must call the Connect method before sending and receiving data.

        //         //TcpClient(String, Int32)
        //         //This constructor creates a new TcpClient and makes a synchronous connection attempt to the provided
        //         //host name and port number. The underlying service provider will assign the most appropriate local
        //         //IP address and port number. TcpClient will block until it either connects or fails. This constructor
        //         //allows you to initialize, resolve the DNS host name, and connect in one convenient step.

        //         _client = new TcpClient()
        //         //_client = new TcpClient(ipEndPoint)
        //         //_client = new TcpClient(_host, _port)
        //         {
        //             // Set some properties.
        //             SendTimeout = _config.ResponseTime,
        //             ReceiveTimeout = _config.ResponseTime,
        //             SendBufferSize = _config.BufferSize,
        //             ReceiveBufferSize = _config.BufferSize
        //         };

        //         //_logger.Debug("[Client] Try connecting to server");
        //         //_client.Connect(_host, _port);
        //         ////var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_config.ResponseTime));
        //         ////await client.ConnectAsync(_host, _port, cts.Token);
        //         //_logger.Debug("[Client] Connected to server");
        //     }
        //     catch (Exception ex)
        //     {
        //         //var t = ex.GetType();
        //         //msg = $"{ex.Message}  --- Invalid comm args";
        //         //stat = OpStatus.Error;
        //         //Dispose();

        //         var res = ProcessException(ex);
        //         msg = res.msg;
        //         stat = res.stat;
        //     }

        //     return (stat, msg);
        // }

        public (OpStatus stat, string msg) Send(string data)
        {
            OpStatus stat = OpStatus.Success;
            string msg = "";

            try
            {
                stat = EnsureConnect();
                // if (!_client.Connected)
                // {
                //     _logger.Debug("[Client] Try reconnecting to server");
                //     _client.Connect(_host, _port);
                //     // await _client.ConnectAsync(_host, _port);
                //     _logger.Debug("[Client] Reconnected to server");
                // }

                using var stream = AltStream ?? _client!.GetStream();

                bool done = false;
                var tx = Utils.StringToBytes(data);
                int num = tx.Count();
                int ind = 0;
                
                while (!done)
                {
                    // Do a chunk.
                    int tosend = num - ind >= _client!.SendBufferSize ? _client.SendBufferSize : num - ind;

                    _logger.Trace($"[Client] Sending [{tosend}]");

                    // If the send time-out expires, Write() throws SocketException.
                    stream.Write(tx, ind, tosend);

                    ind += tosend;
                    done = ind >= num;
                }
            }
            catch (Exception e)
            {
                stat = ProcessException(e);
            }

            return (stat, msg);
        }

        public (OpStatus stat, string msg, string data) Receive()
        {
            OpStatus stat = OpStatus.Success;
            string msg = "";
            string data = "";

            try
            {
                stat = EnsureConnect();
                // if (!_client.Connected)
                // {
                //     _logger.Debug("[Client] Try reconnecting to server");
                //     _client.Connect(_host, _port);
                //     // await _client.ConnectAsync(_host, _port);
                //     _logger.Debug("[Client] Reconnected to server");
                // }

                bool rcvDone = false;
                int totalRx = 0;
                byte[] rx = new byte[_config!.BufferSize];

                using var stream = AltStream ?? _client.GetStream();

                while (!rcvDone)
                {
                    // Get response. If the read time-out expires, Read() throws IOException.
                    int byteCount = stream.Read(rx, totalRx, _config!.BufferSize - totalRx);

                    if (byteCount == 0)
                    {
                        rcvDone = true;
                    }
                    else if (totalRx >= _config.BufferSize)
                    {
                        rcvDone = true;
                        _logger.Warn("TcpComm rx buffer overflow");
                    }
                    else
                    {
                        data = Utils.BytesToString(rx);
                    }
                }

                _logger.Trace($"[Client] Server response was [{totalRx}]");
            }
            catch (Exception e)
            {
                var res = ProcessException(e);
            }

            return (stat, msg, data);
        }

        void Reset()
        {
            // Reset comms, resource management.
            _client.Close();
            // # Reset watchdog.
            //sendts = 0
            // // # Clear queue.
            // while not _qCli.empty():
            //     _qCli.get()
        }        
        #endregion


        OpStatus EnsureConnect()
        {
            var stat = OpStatus.Success;

            if (!_client.Connected)
            {
                try
                {
                    _logger.Debug("[Client] Try connecting to server");
                    _client.Connect(_host, _port);
                    //var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_config.ResponseTime));
                    //await client.ConnectAsync(_host, _port, cts.Token);
                    _logger.Debug("[Client] Connected to server");
                }
                catch (Exception e)
                {
                    stat = ProcessException(e);
                    //msg = res.msg;
                    //stat = res.stat;
                }
            }

            return stat;
        }

        OpStatus ProcessException(Exception e)
        {
            // ArgumentNullException - The buffers parameter was null.
            // ArgumentException - An argument was invalid. The Buffer or BufferList properties on the e parameter must reference valid buffers. One or the other of these properties may be set, but not both at the same time.
            // InvalidOperationException - A socket operation was already in progress using the SocketAsyncEventArgs object specified in the e parameter.

            // SocketException - An error occurred when attempting to access the socket. 
            //     or  The Socket is not yet connected or was not obtained via an Accept(), AcceptAsync(SocketAsyncEventArgs),or BeginAccept, method.
                    // Some are expected and recoverable.
                        // stat = OpStatus.Timeout;
                        // stat = OpStatus.Error;

            // OperationCanceledException - The cancellation token was canceled. This exception is stored into the returned task.
                    // Usually connect timeout. Ignore and retry later.

            // IOException
                    // Usually receive timeout. Ignore and retry later.

            // ObjectDisposedException - The Socket has been closed.

            OpStatus stat = OpStatus.Success;

            switch (e)
            {
                case (OperationCanceledException ex):
                    // Usually connect timeout. Ignore and retry later.
                    stat = OpStatus.Timeout;
                    _logger.Debug($"OperationCanceledException: Timeout: {ex.Message}");
                    break;

                case (SocketException ex):
                    // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                    int[] valid = [10053, 10054, 10060, 10061, 10064];
                    if (valid.Contains(ex.NativeErrorCode))
                    {
                        // Ignore and retry later.
                        stat = OpStatus.Timeout;
                        _logger.Debug($"SocketException: Timeout: {ex.NativeErrorCode}");
                    }
                    else
                    {
                        stat = OpStatus.Error;
                        _logger.Error($"SocketException: Error: {ex}");
                    }
                    break;

                case (IOException ex):
                    // Usually receive timeout. Ignore and retry later.
                    stat = OpStatus.Timeout;
                    _logger.Debug($"IOException: Timeout: {ex.Message}");
                    break;

                default:
                    // Other errors are considered fatal.
                    stat = OpStatus.Error;
                    _logger.Error($"Fatal exception: {e}");
                    break;
            }

            return stat;
        }

        public void Dispose()
        {
            _client?.Close();
            _client?.Dispose();
            _client = null;
        }

        void IComm.Reset()
        {
            Reset();
        }
    }
}

