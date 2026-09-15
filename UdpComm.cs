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
        readonly bool _send = false; // TODO1 prob a bad idea to do this?
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
                if (config.Count > 3)
                {
                    if (config[3].Equals("send", StringComparison.CurrentCultureIgnoreCase))
                    {
                        _send = true;
                    }
                    else
                    {
                        throw new Exception(config[3]);
                    }
                }
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
                "udp host port [send]",
                "host: like 127.0.0.1",
                "port: port to listen",
                "send: make this a sender instead of listener"
            ];
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return $"UdpComm {_host}:{_port} sender:{_send}";
        }
        #endregion

        #region IComm implementation
        /// <see cref="IComm"/>
        public void Send(byte[] td)
        {
            if (!_send)
            {
                throw new InvalidOperationException("Not configured to send");
            }
            _qSend.Enqueue([]);
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

            while (!done)
            {
                token.ThrowIfCancellationRequested();

                //=========== Send ============//
                if (_send)
                {
                    if (_qSend.TryDequeue(out byte[]? td))
                    {
                        try
                        {
                            using var client = new UdpClient(_port);
                            client.Send(td);
                        }
                        catch (Exception e)
                        {
                            // What happened?
                            var res = Common.ProcessException(e);

                            switch (res.cst)
                            {
                                case CommState.Ok:
                                case CommState.Timeout:
                                case CommState.Recoverable:
                                    // Continue running. TODO1 or not? send failed...
                                    break;

                                case CommState.Stop:
                                    done = true;
                                    break;

                                case CommState.Fatal:
                                    throw (res.e);
                            }
                        }
                    }
                }

                //=========== Receive ==========//
                else
                {
                    try
                    {
                        using var client = new UdpClient(_port);
                        IPEndPoint ep = new(IPAddress.Any, _port);
                        byte[] bytes = client.Receive(ref ep);
                        if (bytes.Length > 0)
                        {
                            //Console.WriteLine($"Received broadcast from {ep} :");
                            progress.Report(bytes);
                        }
                    }
                    catch (Exception e)
                    {
                        // What happened?
                        var res = Common.ProcessException(e);

                        switch (res.cst)
                        {
                            case CommState.Ok:
                            case CommState.Timeout:
                            case CommState.Recoverable:
                                // Continue running.
                                break;

                            case CommState.Stop:
                                done = true;
                                break;

                            case CommState.Fatal:
                                throw (res.e);
                        }
                    }
                }

                // Don't be greedy.
                await Task.Delay(10, token);
            }
        }
        #endregion
    }
}