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
    // internal class TcpComm(ITcpClient? tclient) : IComm
    internal class TcpComm : IComm
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("TCP");
        // readonly ITcpPort _tcpClient = tclient ?? new RealTcpClient();
        string _host = "???";
        int _port = 0;
        Config? _config;
        const byte POLL_REQ = 0;
        #endregion

        #region IComm implementation
        public (OpStatus stat, string rx) Init(Config config)
        {
            _config = config;
            OpStatus stat;
            string rx = "";

            try
            {
                // Parse the args. "127.0.0.1 59120"
                var parts = _config.Args.SplitByToken(" ");
                stat = parts.Count == 2 ? OpStatus.Success : OpStatus.Error;

                _host = parts[0];
                _port = int.Parse(parts[1]);

                IPEndPoint ipEndPoint = new(IPAddress.Parse(_host), _port);

                // Test args by creating client.
                using var client = new TcpClient(_host, _port);
            }
            catch (Exception)
            {
                rx = "Invalid comm args";
                stat = OpStatus.Error;
            }

            return (stat, rx);
        }

        public (OpStatus stat, string rx) Send(string? tx) { return WriteAsync(tx).Result; }

        public void Dispose()
        {
            //_tcpClient?.Close();
            //_tcpClient?.Dispose();
            //_tcpClient = null;
        }
        #endregion

         /// <summary>
        /// Does actual work of sending/receiving.
        /// </summary>
        /// <param name="tx"></param>
        /// <returns>OpStatus and Response populated.</returns>
        public async Task<(OpStatus stat, string rx)> WriteAsync(string? tx)
        {
            OpStatus stat = OpStatus.Success;
            var rx = "";
            _logger.Debug($"[Client] Writing request [{tx}]");

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
                using var stream = client.GetStream();
                // using var stream = new ScriptStream("TODO1", []);
                // check for poll.
                byte[] bytes = tx is null ? [POLL_REQ] : Encoding.UTF8.GetBytes(tx);

                bool sendDone = false;
                int num = bytes.Length;
                int ind = 0;
                
                while (!sendDone)
                {
                    // Do a chunk.
                    int tosend = num - ind >= client.SendBufferSize ? client.SendBufferSize : num - ind;

                    _logger.Trace($"[Client] Sending [{tosend}]");

                    // If the send time-out expires, WriteAsync() throws SocketException.
                    await stream.WriteAsync(bytes.AsMemory(ind, tosend));

                    ind += tosend;
                    sendDone = ind >= num;
                }

                /////// Receive ////////
                List<string> parts = [];
                bool rcvDone = false;

                while (!rcvDone)
                {
                    // Get response.
                    var buffer = new byte[client.ReceiveBufferSize];

                    // If the read time-out expires, ReadAsync() throws IOException.
                    var byteCount = await stream.ReadAsync(buffer);

                    if (byteCount > 0)
                    {
                        var s = Encoding.UTF8.GetString(buffer, 0, byteCount);
                        parts.Add(s);
                    }
                    else
                    {
                        rcvDone = true;
                    }
                }

                rx = string.Join("", parts);

                _logger.Debug($"[Client] Server response was [{rx}]");
            }
            catch (OperationCanceledException e)
            {
                rx = "Usually connect timeout.";
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
                    rx = "Usually send timeout.";
                    stat = OpStatus.Timeout;
                }
                else
                {
                    rx = $"Hard socket error: {e.Message}";
                    stat = OpStatus.Error;
                }
            }
            catch (IOException e)
            {
                // Usually receive timeout.
                // Ignore and retry later.
                _logger.Debug($"{e.Message}: {e}");
                rx = "Usually receive timeout.";
                stat = OpStatus.Timeout;
            }
            catch (Exception e)
            {
                // Other errors are considered fatal.
                _logger.Error($"Fatal error:{e}");
                rx = $"Fatal error: {e.Message}";
                stat = OpStatus.Error;
            }

            return (stat, rx);
        }
    }
}

