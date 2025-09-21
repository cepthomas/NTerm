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
using NTerm;


namespace TestX
{
    public class UdpSender
    {
        string _host;
        int _port;
        byte _delim;
        CancellationTokenSource _ts;

        public UdpSender(int port, byte delim, CancellationTokenSource ts)
        {
            _port = port;
            _delim = delim;
            _ts = ts;
            _host = "127.0.0.1";
        }


        /// <summary>
        /// Test udp in continuous mode.
        /// </summary>
        public bool Run()
        {
            bool err = false;

            PrintLine(Cat.Info, $"Udp using port: {port}");

            var lines = File.ReadAllLines(@"C:\Dev\Apps\NTerm\Test\ross_2.txt");
            int ind = 0;

            //=========== Connect ============//
            UdpClient client = new UdpClient();

            client.Connect(_host, _port);
            PrintLine(Cat.Info, "Client has connected");

            while (!ts.Token.IsCancellationRequested)
            {
                try
                {
                    //=========== Send ===============//
                    // Send a burst of X messages spaced Y apart every Z seconds.
                    string send = lines[ind];

                    byte[] bytes = Encoding.Default.GetBytes(send).Append(_delim).ToArray();
                    client.Send(bytes, bytes.Length);

                    ind += 1;

                    if (ind > lines.Count)
                    {
                        ts.Cancel();
                    }
                    else
                    {
                        // Pacing.
                        System.Threading.Thread.Sleep(ind % 10 == 0 ? 500 : 5);
                    }
                }
                catch (Exception e)
                {
                    PrintLine(Cat.Error, $"Exception: {e}");
                    err = true;
                    ts.Cancel();
                }
                // catch (SocketException e)
                // catch (IOException e)
            }

            return err;
        }
    }
}
