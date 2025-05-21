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


// https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient


namespace NTerm
{
    /// <summary>TCP comm.</summary>
    /// <see cref="IComm"/>
    internal class TcpComm : IComm
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("TCP");
        readonly Config _config;
        // TcpClient _client;
        readonly string _host;
        readonly int _port;
        #endregion

        /// <summary>Measurer.</summary>
        //readonly TimingAnalyzer _tan = new();

        #region Lifecycle
        public TcpComm(Config config)
        {
            try
            {
                _config = config;

                // Parse the args: "127.0.0.1 59120"
                var parts = _config.Args;

                _host = parts[0];
                _port = int.Parse(parts[1]);

                // IPEndPoint ipEndPoint = new(IPAddress.Parse(_host), _port);

                // _client = new TcpClient()
                // {
                //     // Set some properties.
                //     SendTimeout = _config.ResponseTime,
                //     ReceiveTimeout = _config.ResponseTime,
                //     SendBufferSize = _config.BufferSize,
                //     ReceiveBufferSize = _config.BufferSize
                // };
            }
            catch (Exception e)
            {
                var msg = $"Invalid args: {e.Message}";
                throw new ArgumentException(msg);
            }
        }

        public void Dispose()
        {
            // _client?.Close();
            // _client?.Dispose();
            // _client = null;
        }
        #endregion

        #region IComm implementation
        //public Stream? AltStream { get; set; } = null;

        public (OpStatus stat, string msg) Send(string data)
        {
            OpStatus stat = OpStatus.Success;
            string msg = "";

            try
            {
                //_tan.Arm();

                using var client = new TcpClient(_host, _port);
                client.SendTimeout = _config.ResponseTime;
                client.SendBufferSize = _config.BufferSize;

                //     // Set some properties.
                //     SendTimeout = _config.ResponseT4ime,
                //     SendBufferSize = _config.BufferSize,

                using var stream = client.GetStream();

                bool done = false;
                var tx = Utils.StringToBytes(data);
                int num = tx.Length;
                int ind = 0;
                
                while (!done)
                {
                    // Do a chunk.
                    int tosend = num - ind >= client!.SendBufferSize ? client.SendBufferSize : num - ind;

                    // If the send time-out expires, Write() throws SocketException.
                    stream.Write(tx, ind, tosend);

                    ind += tosend;
                    done = ind >= num;
                }

                //if (_tan.Grab())
                //{
                //    _logger.Info($"Send(): {_tan.Dump()}");
                //    _tan.Stop();
                //}


                // stat = EnsureConnect();

                // if (stat == OpStatus.Success)
                // {
                //     using var stream = AltStream ?? _client!.GetStream();

                //     bool done = false;
                //     var tx = Utils.StringToBytes(data);
                //     int num = tx.Length;
                //     int ind = 0;
                    
                //     while (!done)
                //     {
                //         // Do a chunk.
                //         int tosend = num - ind >= _client!.SendBufferSize ? _client.SendBufferSize : num - ind;

                //         // If the send time-out expires, Write() throws SocketException.
                //         stream.Write(tx, ind, tosend);

                //         ind += tosend;
                //         done = ind >= num;
                //     }
                // }

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
                //_tan.Arm();

                //https://stackoverflow.com/questions/17118632/how-to-set-the-timeout-for-a-tcpclient
                using var client = new TcpClient(_host, _port);
                //using var client = new TcpClient();// _host, _port);
                // Set some properties.
                client.ReceiveTimeout = 10;// _config.ResponseTime;
                client.ReceiveBufferSize = _config.BufferSize;
                //client.Connect(_host, _port);  // await client.ConnectAsync(_host, _port, cts.Token);

                using var stream = client.GetStream();


                bool rcvDone = false;
                int totalRx = 0;
                byte[] rx = new byte[_config!.BufferSize];

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

                //if (_tan.Grab())
                //{
                //    _logger.Info($"Send(): {_tan.Dump()}");
                //    _tan.Stop();
                //}

                // stat = EnsureConnect();

                // if (stat == OpStatus.Success)
                // {
                //     bool rcvDone = false;
                //     int totalRx = 0;
                //     byte[] rx = new byte[_config!.BufferSize];

                //     using var stream = AltStream ?? _client.GetStream();

                //     while (!rcvDone)
                //     {
                //         // Get response. If the read time-out expires, Read() throws IOException.
                //         int byteCount = stream.Read(rx, totalRx, _config!.BufferSize - totalRx);

                //         if (byteCount == 0)
                //         {
                //             rcvDone = true;
                //         }
                //         else if (totalRx >= _config.BufferSize)
                //         {
                //             rcvDone = true;
                //             _logger.Warn("TcpComm rx buffer overflow");
                //         }
                //         else
                //         {
                //             data = Utils.BytesToString(rx);
                //         }
                //     }
                // }
            }
            catch (Exception e)
            {
                stat = ProcessException(e);
            }

            return (stat, msg, data);
        }

        public void Reset()
        {
            // Reset comms, resource management.
            //_client.Close();
        }
        #endregion

        #region Private stuff
        // OpStatus EnsureConnect()
        // {
        //     var stat = OpStatus.Success;

        //     if (!_client.Connected)
        //     {
        //         try
        //         {
        //             _client.Connect(_host, _port);  // await client.ConnectAsync(_host, _port, cts.Token);
        //         }
        //         catch (Exception e)
        //         {
        //             stat = ProcessException(e);
        //         }
        //     }

        //     return stat;
        // }

        OpStatus ProcessException(Exception e)
        {
            OpStatus stat;

            switch (e)
            {
                case OperationCanceledException ex:
                    // Usually connect timeout. Ignore and retry later.
                    stat = OpStatus.Timeout;
                    //_logger.Debug($"OperationCanceledException: Timeout: {ex.Message}");
                    break;

                case SocketException ex:
                    // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                    int[] valid = [10053, 10054, 10060, 10061, 10064];
                    if (valid.Contains(ex.NativeErrorCode))
                    {
                        // Ignore and retry later.
                        stat = OpStatus.Timeout;
                        //_logger.Debug($"SocketException: Timeout: {ex.NativeErrorCode}");
                    }
                    else
                    {
                        stat = OpStatus.Error;
                        _logger.Error($"SocketException: Error: {ex}");
                    }
                    break;

                case IOException ex:
                    // Usually receive timeout. Ignore and retry later.
                    stat = OpStatus.Timeout;
                    //_logger.Debug($"IOException: Timeout: {ex.Message}");
                    break;

                case ObjectDisposedException ex:
                    stat = OpStatus.Error;
                    _logger.Error($"ObjectDisposedException: {ex.Message}");
                    break;

                default:
                    // Other errors are considered fatal.
                    stat = OpStatus.Error;
                    _logger.Error($"Fatal exception: {e}");
                    break;
            }

            return stat;
        }
        #endregion
    }
}
