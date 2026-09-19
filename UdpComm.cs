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
        readonly ConcurrentQueue<byte[]> _qSend = new();
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
               var msg = $"Invalid arg: {e.Message}";
               throw new ConfigException(msg);
           }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
        }

        /// <summary>Send help.</summary>
        public static List<string> Usage()
        {
            return
            [
                "udp host port",
                "host: like 127.0.0.1",
                "port: port to listen",
            ];
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return $"UdpComm {_host}:{_port}";
        }
        #endregion

        #region IComm implementation
        /// <see cref="IComm"/>
        public void Send(string td)
        {
            throw new NotImplementedException("UDP is listen only");
        }

        /// <see cref="IComm"/>
        public void Reset()
        {
        }

        /// <summary>Main work loop.</summary>
        /// <see cref="IComm"/>
        public async Task Run(CancellationToken token, IProgress<byte[]> progress)
        {
            bool done = false;

            while (!done && !token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();

                //=========== Receive ==========//
                try
                {
                    using var client = new UdpClient(_port);
                    IPEndPoint ep = new(IPAddress.Any, _port);
                    byte[] bytes = client.Receive(ref ep);
                    if (bytes.Length > 0)
                    {
                        // Just pass along.
                        progress.Report(bytes);
                    }
                }
                catch (Exception e)
                {
                    // What happened?
                    done = Common.ProcessException(e);
                }

                // Don't be greedy.
                await Task.Delay(10, token);
            }
        }
        #endregion
    }
}