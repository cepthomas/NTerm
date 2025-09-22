using System;
//using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Ephemera.NBagOfTricks;
using System.Linq;
//using NTerm;


namespace Test
{
    public class UdpSender
    {
        #region Fields
        readonly string _host;
        readonly int _port;
        readonly byte _delim;
        readonly CancellationTokenSource _ts;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="delim"></param>
        /// <param name="ts"></param>
        public UdpSender(int port, byte delim, CancellationTokenSource ts)
        {
            _port = port;
            _delim = delim;
            _ts = ts;
            _host = "127.0.0.1";

            Console.WriteLine($"Udp using {_host}:{_port}");
        }

        /// <summary>
        /// Test udp in continuous mode.
        /// </summary>
        public bool Run()
        {
            bool err = false;

            while (!_ts.Token.IsCancellationRequested)
            {
                try
                {
                    var lines = File.ReadAllLines(@"C:\Dev\Apps\NTerm\Test\ross_2.txt").ToList();
                    int ind = 0;

                    //=========== Connect ============//
                    using UdpClient client = new();
                    client.Connect(_host, _port);
                    Console.WriteLine("Client has connected");

                    //=========== Send ===============//
                    // Send a burst of X messages spaced Y apart every Z seconds.

                    string send = lines[ind];
                    byte[] bytes = [.. Encoding.Default.GetBytes(send), _delim];
                    client.Send(bytes, bytes.Length);

                    // Next.
                    ind += 1;
                    if (ind >= lines.Count)
                    {
                        _ts.Cancel();
                    }
                    else
                    {
                        // Pacing.
                        Thread.Sleep(ind % 10 == 0 ? 500 : 5);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception: {e}");
                    err = false;
                    _ts.Cancel();
                }
            }

            return err;
        }
    }
}
