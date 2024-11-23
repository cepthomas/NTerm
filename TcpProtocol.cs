
using Ephemera.NBagOfTricks.Slog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NTerm
{
    // https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/socket-services

    // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient?view=net-8.0

    // https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket?view=net-8.0





    internal class TcpProtocol : IProtocol
    {
        #region Fields
        TcpClient? _client = null;

        string _host = "???";
        int _port = 0;
        IPEndPoint _ipEndPoint;

        // Server must connect or reply to commands in msec.
        const int SERVER_RESPONSE_TIME = 500;

        const int BUFFER_SIZE = 4096;

        // bool _run = true;

        // string _rcvBuffer = "";

        // readonly Stopwatch _watch = new();

        // long _sendts = 0;

        readonly Logger _logger = LogManager.CreateLogger("TcpProtocol");

        // readonly ConcurrentQueue<string?> _sendQ = new();

        // readonly ConcurrentQueue<string?> _rcvQ = new();

        #endregion

        public TcpProtocol(string host, int port)
        {
            _host = host;
            _port = port;
            _ipEndPoint = new(IPAddress.Parse(host), port);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        // IProtocol implementation
        public string? Send(string request)
        {
            string? res = SendAsync(request).Result;
            return res;
        }

        public async Task<string?> SendAsync(string request)
        {
            string? res = null;

            try
            {
                //// connect
                //using var tcpClient = new TcpClient();
                //await tcpClient.ConnectAsync(_host, _port);
                //// send
                //using var networkStream = tcpClient.GetStream();
                //string ClientRequestString = "Some HTTP request here";
                //byte[] ClientRequestBytes = Encoding.UTF8.GetBytes(ClientRequestString);
                //await networkStream.WriteAsync(ClientRequestBytes, 0, ClientRequestBytes.Length);
                //// receive
                //var bufferX = new byte[4096];
                //var byteCountX = await networkStream.ReadAsync(bufferX, 0, bufferX.Length);
                //var response = Encoding.UTF8.GetString(bufferX, 0, byteCountX);
                //return response;


                /////// Connect ////////
                using var client = new TcpClient();

                // Set some properties. TODO from config or settings?
                client.SendTimeout = SERVER_RESPONSE_TIME;
                client.ReceiveTimeout = SERVER_RESPONSE_TIME;
                client.SendBufferSize = BUFFER_SIZE;
                client.ReceiveBufferSize = BUFFER_SIZE;


                _logger.Debug("[Client] Try connecting to server");

                var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));// SERVER_RESPONSE_TIME);
                await client.ConnectAsync(_host, _port, cts.Token);
                // This method stores in the task it returns all non-usage exceptions that the method's synchronous
                // counterpart can throw. If an exception is stored into the returned task, that exception will be
                // thrown when the task is awaited. Usage exceptions, such as ArgumentException, are still thrown synchronously.

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
                    //do a chunk of SendBufferSize
                    int tosend = num - ind >= client.SendBufferSize ? client.SendBufferSize : num - ind;

                    _logger.Debug($"[Client] Sending [{tosend}]");

                    // If the send time-out expires, TcpClient throws SocketException.
                    await stream.WriteAsync(bytes, ind, tosend);

                    ind += tosend;
                    sendDone = ind >= num;
                }

                /////// Receive ////////

                bool rcvDone = false;
                List<string> response = [];

                while (!rcvDone)
                {
                    // Get response.
                    var buffer = new byte[client.ReceiveBufferSize];

                    // If the read time-out expires, TcpClient throws IOException.
                    var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);

                    if (byteCount > 0)
                    {
                        var s = Encoding.UTF8.GetString(buffer, 0, byteCount);
                        response.Add(s);
                    }
                    else
                    {
                        rcvDone = true;
                    }
                }

                res = string.Join("", response);

                _logger.Debug($"[Client] Server response was [{res}]");

            }
            catch (OperationCanceledException e)
            {
                // Usually connect timeout.
                _logger.Debug($"{e.Message}: {e}");
                res = null;
            }
            catch (SocketException e)
            {
                // Usually send timeout.
                _logger.Debug($"{e.Message}: {e.NativeErrorCode}");
                // https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                // Some are expected and recoverable:
                // WSAECONNABORTED 10053
                // WSAECONNRESET 10054
                // WSAETIMEDOUT 10060
                // WSAECONNREFUSED 10061
                // WSAEHOSTDOWN 10064
                // Ignore and retry later.
                //Reset();
                res = null;
            }
            catch (IOException e)
            {
                // Usually receive timeout.
                // Tell client to retry later.
                _logger.Debug($"{e.Message}: {e}");
                res = null;
            }
            catch (Exception e)
            {
                // Other errors are considered fatal.
                _logger.Error($"Fatal error:{e}");
                //Reset();
                //run = false;
                res = null;
            }

            return res;
        }
    }
}

