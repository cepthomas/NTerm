using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;



namespace NTerm
{
    /// <summary>UDP comm.</summary>
    /// <see cref="IComm"/>
    internal class UdpComm : IComm
    {
        #region Fields
        readonly string _host;
        readonly int _port;
        readonly ConcurrentQueue<byte[]> _qRecv = new();
        const int CONNECT_TIME = 50;
        const int RESPONSE_TIME = 1000;
        const int BUFFER_SIZE = 4096;
        #endregion

        #region Lifecycle
        /// <summary>Constructor.</summary>
        /// <param name="config"></param>
        /// <exception cref="ArgumentException"></exception>
        public UdpComm(List<string> config)
        {
           try
           {
               _host = config[1];
               _port = int.Parse(config[2]);
           }
           catch (Exception e)
           {
               var msg = $"Invalid args: {e.Message}";
               throw new ArgumentException(msg);
           }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return ($"UdpComm {_host} {_port}");
        }
        #endregion

        #region IComm implementation
        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(byte[] td)
        {
            throw new NotSupportedException();
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public byte[]? Receive()
        {
            _qRecv.TryDequeue(out byte[]? res);
            return res;
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Run(CancellationToken token)
        {
            //https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.udpclient

            while (!token.IsCancellationRequested)
            {
                try
                {
                    //=========== Connect ============//
                    using UdpClient client = new(_host, _port);


                    //=========== Send ============//
                    // Not supported.


                    //=========== Receive ==========//
                    bool rcvDone = false;
                    while (!rcvDone)
                    {
                        // Get data.
                        var bytes = client.ReceiveAsync(token);
                        if (bytes.Result.Buffer.Length > 0)
                        {
                            _qRecv.Enqueue(bytes.Result.Buffer);
                        }
                        else
                        {
                            rcvDone = true;
                        }
                    }
                }
                catch (Exception e)
                {
                    //var stat = Utils.ProcessException(e, _logger);

                    // Connect:
                    // - SocketException - An error occurred when accessing the socket.
                    // - ArgumentNullException - endPoint is null.
                    // - ObjectDisposedException - The UdpClient is closed.
                    // 
                    // ReceiveAsync:
                    // - ObjectDisposedException - The underlying Socket has been closed.
                    // - SocketException - An error occurred when accessing the socket.

                    switch (e)
                    {
                        case SocketException ex: // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                            int[] valid = [10053, 10054, 10060, 10061, 10064];
                            if (valid.Contains(ex.NativeErrorCode))
                            {
                                // Ignore and retry later.
                            }
                            else
                            {
                                // All others are considered fatal - bubble up to App to handle.
                                throw;
                            }
                            break;

                        default: // All others are considered fatal - bubble up to App to handle.
                            throw;
                    }
                }

                // Don't be greedy.
                Thread.Sleep(5);
            }
        }
        #endregion
    }
}
