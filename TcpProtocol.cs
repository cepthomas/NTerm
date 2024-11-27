
using Ephemera.NBagOfTricks.Slog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace NTerm
{
    internal class TcpProtocol : IProtocol
    {
        #region IProtocol implementation
        public int ResponseTime { get; set; } = 500;

        public int BufferSize { get; set; } = 4096;

        public string Response { get; private set; } = "";

        public OpStatus Send(string request) { return SendAsync(request).Result; }
        #endregion

        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("Protocol");
        string _host = "???";
        int _port = 0;
        IPEndPoint _ipEndPoint;
        #endregion

        public TcpProtocol(string host, int port)
        {
            _host = host;
            _port = port;
            _ipEndPoint = new(IPAddress.Parse(host), port);
        }

        public void Dispose()
        {
        }


        public async Task<OpStatus> SendAsync(string request)
        {
            OpStatus res = OpStatus.Success;
            Response = "";

            try
            {
                /////// Connect ////////
                using var client = new TcpClient(_host, _port);

                // Set some properties.
                client.SendTimeout = ResponseTime;
                client.ReceiveTimeout = ResponseTime;
                client.SendBufferSize = BufferSize;
                client.ReceiveBufferSize = BufferSize;

                _logger.Debug("[Client] Try connecting to server");
                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(ResponseTime));
                // Throws OperationCanceledException if timeout or several other failure types.
                await client.ConnectAsync(_host, _port, cts.Token);

                _logger.Debug("[Client] Connected to server");

                /////// Send ////////
                using var stream = client.GetStream();
                byte[] bytes = Encoding.UTF8.GetBytes(request);
                _logger.Debug($"[Client] Writing request [{request}]");

                bool sendDone = false;
                int num = bytes.Length;
                int ind = 0;
                while (!sendDone)
                {
                    // Do a chunk.
                    int tosend = num - ind >= client.SendBufferSize ? client.SendBufferSize : num - ind;

                    _logger.Debug($"[Client] Sending [{tosend}]");

                    // If the send time-out expires, WriteAsync() throws SocketException.
                    await stream.WriteAsync(bytes, ind, tosend);

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
                    var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);

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

                Response = string.Join("", parts);

                _logger.Debug($"[Client] Server response was [{Response}]");
            }
            catch (OperationCanceledException e)
            {
                Response = "Usually connect timeout.";
                res = OpStatus.Timeout;
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
                    Response = "Usually send timeout.";
                    res = OpStatus.Timeout;
                }
                else
                {
                    Response = $"Hard socket error: {e.Message}";
                    res = OpStatus.Error;
                }
            }
            catch (IOException e)
            {
                // Usually receive timeout.
                // Ignore and retry later.
                _logger.Debug($"{e.Message}: {e}");
                Response = "Usually receive timeout.";
                res = OpStatus.Timeout;
            }
            catch (Exception e)
            {
                // Other errors are considered fatal.
                _logger.Error($"Fatal error:{e}");
                Response = $"Fatal error: {e.Message}";
                res = OpStatus.Error;
            }

            return res;
        }
    }
}

