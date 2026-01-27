using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


// https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient

namespace NTerm
{
    /// <summary>TCP comm.</summary>
    /// <see cref="IComm"/>
    internal class TcpComm : IComm
    {
        #region Fields
        readonly string _host;
        readonly int _port;
        readonly ConcurrentQueue<byte[]> _qSend = new();
        readonly ConcurrentQueue<object> _qRecv = new();
        const int CONNECT_TIME = 50;
        const int RESPONSE_TIME = 1000;
        const int BUFFER_SIZE = 4096;
        #endregion

        /// <summary>Module logger.</summary>
        //readonly Logger _logger = LogManager.CreateLogger("TCP");

        #region Lifecycle
        /// <summary>Constructor.</summary>
        /// <param name="config"></param>
        /// <exception cref="ConfigException"></exception>
        public TcpComm(List<string> config)
        {
            try
            {
                _host = config[1];
                _port = int.Parse(config[2]);
            }
            catch (Exception e)
            {
                var msg = $"Invalid args: {e.Message}";
                throw new ConfigException(msg);
            }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return ($"TcpComm {_host}:{_port}");
        }
        #endregion

        #region IComm implementation
        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public CommState State { get; private set; }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(byte[] req)
        {
            _qSend.Enqueue(req);
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public object? GetReceive()
        {
            _qRecv.TryDequeue(out object? res);
            return res;
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }

        ///// <summary>IComm implementation.</summary>
        ///// <see cref="IComm"/>
        //public event EventHandler<NotifEventArgs>? Notif;
        #endregion

        /// <summary>Main work loop.</summary>
        /// <see cref="IComm"/>
        public void Run(CancellationToken token)
        {
            //_logger.Info("Run start");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    //=========== Work to do? ============//
                    if (_qSend.TryDequeue(out byte[]? td))
                    {
                        bool sendDone = false;
                        int numToSend = td.Length;
                        int ind = 0;


                        //=========== Connect ============//
                        using var client = new TcpClient();
                        client.SendTimeout = RESPONSE_TIME;
                        client.SendBufferSize = BUFFER_SIZE;

                        var task = client.ConnectAsync(_host, _port);
                        if (!task.Wait(CONNECT_TIME, token))
                        {
                            throw new TimeoutException();
                           //return (OpStatus.ConnectTimeout, "", resp); // TODO ?
                        }
                        using var stream = client.GetStream();


                        //=========== Send ============//
                        while (!sendDone)
                        {
                            // Do a chunk.
                            int tosend = numToSend - ind >= client!.SendBufferSize ? client.SendBufferSize : numToSend - ind;

                            // If the send time-out expires, Write() throws SocketException.
                            stream.Write(td, ind, tosend);

                            ind += tosend;
                            sendDone = ind >= numToSend;
                        }


                        //=========== Receive ==========//
                        bool rcvDone = false;
                        byte[] rxData = new byte[BUFFER_SIZE];

                        while (!rcvDone)
                        {
                            // Get response. If the read time-out expires, Read() throws IOException.
                            int byteCount = stream.Read(rxData, 0, BUFFER_SIZE);

                            if (byteCount > 0)
                            {
                                var rx = rxData.Subset(0, byteCount);
                                _qRecv.Enqueue(rx);
                            }
                            else
                            {
                                rcvDone = true;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    //_logger.Exception(e);



                    State = Utils.ProcessException(e);
//                    encode error info and return with ESC

                }

                // Don't be greedy.
                Thread.Sleep(10);
            }
        }

        // #region Internals
        // /// <summary>
        // /// Handle errors.
        // /// </summary>
        // /// <param name="e"></param>
        // /// <returns></returns>
        // bool ProcessException(Exception e)
        // {
        //     // ConnectAsync:
        //     // - ArgumentNullException - The host parameter is null. 
        //     // - ArgumentOutOfRangeException - The port parameter is not between MinPort and MaxPort.
        //     // - SocketException - An error occurred when accessing the socket.
        //     // - ObjectDisposedException - TcpClient is closed.
        //     // - OperationCanceledException - The cancellation token was canceled. Ex in returned task. NORMAL-ignore
        //     // GetStream:
        //     // - InvalidOperationException - The TcpClient is not connected to a remote host.  RETRY?
        //     // - ObjectDisposedException - The TcpClient has been closed.
        //     // stream.Write:
        //     // - InvalidOperationException - The NetworkStream does not support writing.
        //     // - IOException - An error occurred when accessing the socket. -or- There was a failure while writing to the network.  RETRY?
        //     // - ObjectDisposedException - The NetworkStream is closed.  RETRY?
        //     // stream.Read:
        //     // - InvalidOperationException - The NetworkStream does not support reading.
        //     // - IOException - An error occurred when accessing the socket. -or- There is a failure reading from the network.  RETRY?
        //     // - ObjectDisposedException - The NetworkStream is closed.  RETRY?

        //     bool fatal = false;

        //     // Async ops carry the original exception in inner.
        //     if (e is AggregateException)
        //     {
        //         e = e.InnerException ?? e;
        //     }

        //     switch (e)
        //     {
        //         case OperationCanceledException: // Usually connect timeout. Ignore and retry later.
        //             break;

        //         case SocketException ex: // Some are expected and recoverable.
        //             // https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
        //             int[] valid = [10053, 10054, 10060, 10061, 10064];
        //             if (valid.Contains(ex.NativeErrorCode))
        //             {
        //                 // Ignore and retry later.
        //             }
        //             else
        //             {
        //                 // Just Notify/log and carry on.
        //                 Notif?.Invoke(this, new(Cat.Log, e.Message));
        //             }
        //             break;

        //         case IOException: // Usually receive timeout. Ignore and retry later.
        //             break;

        //         default:
        //             // Just Notify/log and carry on.
        //             Notif?.Invoke(this, new(Cat.Log, e.Message));
        //             break;
        //     }

        //     return fatal;
        // }
        // #endregion
    }
}
