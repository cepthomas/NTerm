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
        string _host = "???";
        int _port = 0;
        Config _config = new();
        const byte POLL_REQ = 0;
        #endregion

        #region IComm implementation
        public Stream? AltStream { get; set; } = null;

        public (OpStatus stat, string msg) Init(Config config)
        {
            _config = config;
            OpStatus stat;
            string msg = "";

            try
            {
                // Parse the args. "127.0.0.1 59120"
                var parts = _config.Args;
                stat = parts.Count == 2 ? OpStatus.Success : OpStatus.Error;

                _host = parts[0];
                _port = int.Parse(parts[1]);

                IPEndPoint ipEndPoint = new(IPAddress.Parse(_host), _port);

                // Test args by creating client.
                using var client = new TcpClient(_host, _port);
            }
            catch (Exception)
            {
                msg = "Invalid comm args";
                stat = OpStatus.Error;
            }

            return (stat, msg);
        }

        public (OpStatus stat, byte[] rx) Send(byte[] tx)
        {
            return SendAsync(tx).Result;
        }

        public void Dispose()
        {
            //_tcpClient?.Close();
            //_tcpClient?.Dispose();
            //_tcpClient = null;
            AltStream?.Dispose();
            AltStream = null;
        }
        #endregion

        /// <summary>
        /// Does actual work of sending/receiving.
        /// </summary>
        /// <param name="tx">What to send.</param>
        /// <returns>OpStatus and Response populated.</returns>
        public async Task<(OpStatus stat, byte[] rx)> SendAsync(byte[] tx)
        {
            OpStatus stat = OpStatus.Success;
            byte[] rx;

            try
            {
                /////// Connect ////////
                using var client = new TcpClient(_host, _port);

                // Set some properties.
                client.SendTimeout = _config.ResponseTime;
                client.ReceiveTimeout = _config.ResponseTime;
                client.SendBufferSize = _config.BufferSize;
                client.ReceiveBufferSize = _config.BufferSize;

                _logger.Debug("[Client] Try connecting to server");
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_config.ResponseTime));
                await client.ConnectAsync(_host, _port, cts.Token);

                _logger.Debug("[Client] Connected to server");

                /////// Send ////////
                using var stream = AltStream ?? client.GetStream();

                bool sendDone = false;
                int num = tx.Length;
                int ind = 0;
                
                while (!sendDone)
                {
                    // Do a chunk.
                    int tosend = num - ind >= client.SendBufferSize ? client.SendBufferSize : num - ind;

                    _logger.Trace($"[Client] Sending [{tosend}]");

                    // If the send time-out expires, WriteAsync() throws SocketException.
                    await stream.WriteAsync(tx);

                    ind += tosend;
                    sendDone = ind >= num;
                }

                /////// Receive ////////
                bool rcvDone = false;
                int totalRx = 0;
                byte[] buffer = new byte[_config.BufferSize];

                while (!rcvDone)
                {
                    // Get response. If the read time-out expires, ReadAsync() throws IOException.
                    int byteCount = await stream.ReadAsync(buffer, totalRx, _config.BufferSize - totalRx);

                    if (byteCount == 0)
                    {
                        rcvDone = true;
                    }
                    else if (totalRx >= _config.BufferSize)
                    {
                        rcvDone = true;
                        _logger.Warn("TcpComm rx buffer overflow");
                    }
                }

                // Package return.
                rx = new byte[totalRx];
                Array.Copy(rx, 0, buffer, 0, totalRx);

                _logger.Trace($"[Client] Server response was [{totalRx}]");
            }
            catch (OperationCanceledException e)
            {
                rx = Utils.StringToBytes("Usually connect timeout.");
                stat = OpStatus.Timeout;
                _logger.Debug($"{e.Message}: {e}");
            }
            catch (SocketException e)
            {
                _logger.Debug($"{e.Message}: {e.NativeErrorCode}");
                // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                int[] valid = [10053, 10054, 10060, 10061, 10064];
                if (valid.Contains(e.NativeErrorCode))
                {
                    // Ignore and retry later.
                    rx = Utils.StringToBytes("Usually send timeout.");
                    stat = OpStatus.Timeout;
                }
                else
                {
                    rx = Utils.StringToBytes($"Hard socket error: {e.Message}");
                    stat = OpStatus.Error;
                }
            }
            catch (IOException e)
            {
                // Usually receive timeout.
                // Ignore and retry later.
                _logger.Debug($"{e.Message}: {e}");
                rx = Utils.StringToBytes("Usually receive timeout.");
                stat = OpStatus.Timeout;
            }
            catch (Exception e)
            {
                // Other errors are considered fatal.
                _logger.Error($"Fatal error:{e}");
                rx = Utils.StringToBytes($"Fatal error: {e.Message}");
                stat = OpStatus.Error;
            }

            return (stat, rx);
        }
    }
}

