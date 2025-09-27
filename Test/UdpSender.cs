using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace Test
{
    public class UdpSender
    {
        #region Fields
        readonly string _host;
        readonly int _port;
        readonly byte _delim;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="delim"></param>
        public UdpSender(int port, byte delim)
        {
            _port = port;
            _delim = delim;
            _host = "127.0.0.1";

            Console.WriteLine($"Udp using {_host}:{_port}");
        }

        /// <summary>
        /// Do one broadcast cycle.
        /// </summary>
        public void Run(CancellationTokenSource ts)
        {
            bool done = false;

            while (!done && !ts.Token.IsCancellationRequested)
            {
                try
                {
                    var tf = Path.Combine(MiscUtils.GetSourcePath(), "ross_2.txt");
                    var lines = File.ReadAllLines(tf).ToList();

                    //=========== Connect ============//
                    using UdpClient client = new();
                    client.Connect(_host, _port);
                    Console.WriteLine("Client has connected");

                    //=========== Send ===============//
                    // Pace response messages to simulate continuous operationn.
                    int ind = 0;
                    while (!done && !ts.Token.IsCancellationRequested)
                    {
                        string send = lines[ind];
                        byte[] bytes = [.. Encoding.Default.GetBytes(send), _delim];
                        client.Send(bytes, bytes.Length);
                        ind += 1;
                        if (ind >= lines.Count)
                        {
                            done = true;
                            //_ts.Cancel();
                        }
                        else
                        {
                            // Pacing.
                            Thread.Sleep(ind % 10 == 0 ? 500 : 5);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception: {e}");
                    done = true;
                    // _ts.Cancel();
                }
            }

            Console.WriteLine($"Udp done");
        }
    }
}
