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
                    _qRecv.Enqueue(e);
                }

                // Don't be greedy.
                Thread.Sleep(10);
            }
        }
        #endregion
    }
}
