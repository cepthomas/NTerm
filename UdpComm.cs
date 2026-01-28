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
        //readonly ConcurrentQueue<byte[]> _qSend = new();
        readonly ConcurrentQueue<object> _qRecv = new();
        const int BUFFER_SIZE = 4096;
        #endregion

        #region Lifecycle
        /// <summary>Constructor.</summary>
        /// <param name="config"></param>
        /// <exception cref="ConfigException"></exception>
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
            return $"UdpComm {_host}:{_port}";
        }
        #endregion

        #region IComm implementation
        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(byte[] td)
        {
            throw new NotImplementedException();
            //_qSend.Enqueue([]);
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
            //=========== Connect ============//
            using var listener = new UdpClient(_port);
            IPEndPoint ep = new(IPAddress.Any, _port);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    //=========== Receive ==========//
                    // sync
                    //byte[] bytes = listener.Receive(ref ep);
                    //Console.WriteLine($"Received broadcast from {ep} :");

                    // async(ish)
                    var task = listener.ReceiveAsync(token);
                    if (task.IsCompleted && task.IsCompletedSuccessfully)
                    {
                        byte[] bytes = task.Result.Buffer;
                        Console.WriteLine($"{Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                    }
                }

                // Don't be greedy.
                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                _qRecv.Enqueue(e);
            }
        }
        #endregion
    }
}
