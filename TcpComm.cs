using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
        // readonly Logger _logger = LogManager.CreateLogger("TCP");
        string _host;
        int _port;
        readonly ConcurrentQueue<string> _qSend = new();
        readonly ConcurrentQueue<byte[]> _qRecv = new();
        const int CONNECT_TIME = 50;
        const int RESPONSE_TIME = 1000;
        const int BUFFER_SIZE = 4096;
        #endregion

        /// <summary>Constructor.</summary>
        /// <param name="config"></param>
        /// <exception cref="IniSyntaxException"></exception>
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
                throw new IniSyntaxException(msg, -1);
            }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(string req)
        {
            _qSend.Enqueue(req);
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
        public void Run(CancellationToken token)
        {
            CommState state = CommState.None;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    //=========== Connect ============//
                    state = CommState.Connect;

                    using var client = new TcpClient();
                    client.SendTimeout = RESPONSE_TIME;
                    client.SendBufferSize = BUFFER_SIZE;

                    var task = client.ConnectAsync(_host, _port);

                    if (!task.Wait(CONNECT_TIME, token))
                    {
                        //return (OpStatus.ConnectTimeout, "", resp);
                    }

                    using var stream = client.GetStream();


                    //=========== Send ============//
                    state = CommState.Send;

                    if (_qSend.TryDequeue(out string? s))
                    {
                        bool sendDone = false;
                        var txData = Utils.StringToBytes(s);
                        int numToSend = txData.Length;
                        int ind = 0;

                        while (!sendDone)
                        {
                            // Do a chunk.
                            int tosend = numToSend - ind >= client!.SendBufferSize ? client.SendBufferSize : numToSend - ind;

                            // If the send time-out expires, Write() throws SocketException.
                            stream.Write(txData, ind, tosend);

                            ind += tosend;
                            sendDone = ind >= numToSend;
                        }
                    }

                    // Any received?
                    //=========== Receive ==========//
                    state = CommState.Recv;

                    bool rcvDone = false;
                    byte[] rxData = new byte[BUFFER_SIZE];

                    while (!rcvDone)
                    {
                        // Get response. If the read time-out expires, Read() throws IOException.
                        int byteCount = stream.Read(rxData, 0, BUFFER_SIZE);

                        if (byteCount == 0)
                        {
                            rcvDone = true;
                        }
                        else
                        {
                            _qRecv.Enqueue(rxData);
                        }
                    }
                
                }
                catch (Exception e)
                {
                    // Async ops carry the original exception in inner.
                    if (e is AggregateException)
                    {
                        e = e.InnerException ?? e;
                    }

                    // ConnectAsync:
                    // - ArgumentNullException - The host parameter is null. 
                    // - ArgumentOutOfRangeException - The port parameter is not between MinPort and MaxPort.
                    // - SocketException - An error occurred when accessing the socket.
                    // - ObjectDisposedException - TcpClient is closed.
                    // - OperationCanceledException - The cancellation token was canceled. Ex in returned task. NORMAL-ignore
                    // GetStream:
                    // - InvalidOperationException - The TcpClient is not connected to a remote host.  RETRY?
                    // - ObjectDisposedException - The TcpClient has been closed.
                    // stream.Write:
                    // - InvalidOperationException - The NetworkStream does not support writing.
                    // - IOException - An error occurred when accessing the socket. -or- There was a failure while writing to the network.  RETRY?
                    // - ObjectDisposedException - The NetworkStream is closed.  RETRY?
                    // stream.Read:
                    // - InvalidOperationException - The NetworkStream does not support reading.
                    // - IOException - An error occurred when accessing the socket. -or- There is a failure reading from the network.  RETRY?
                    // - ObjectDisposedException - The NetworkStream is closed.  RETRY?
                    
                    switch (e)
                    {
                        case OperationCanceledException ex: // Usually connect timeout. Ignore and retry later.
                            //_logger.Debug($"OperationCanceledException: Timeout: {ex.Message}");
                            break;

                        case SocketException ex: // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                            int[] valid = [10053, 10054, 10060, 10061, 10064];
                            if (valid.Contains(ex.NativeErrorCode))
                            {
                                // Ignore and retry later.
                                //_logger.Debug($"SocketException: Timeout: {ex.NativeErrorCode}");
                            }
                            else
                            {
                                // All other errors are considered fatal - bubble up to App to handle.
                                throw;
                            }
                            break;

                        case IOException ex: // Usually receive timeout. Ignore and retry later.
                            //_logger.Debug($"IOException: Timeout: {ex.Message}");
                            break;

                        default: // All other errors are considered fatal - bubble up to App to handle.
                            throw;
                    }
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }
    }}
