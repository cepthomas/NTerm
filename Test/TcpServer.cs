using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;


namespace Test
{
    public class TcpServer
    {
        #region Fields
        readonly string _host;
        readonly int _port;
        readonly byte _delim;
        //const int CONNECT_TIME = 50;
        //const int RESPONSE_TIME = 1000;
        //const int BUFFER_SIZE = 4096;
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="delim"></param>
        /// <param name="ts"></param>
        public TcpServer(int port, byte delim)
        {
            _port = port;
            _delim = delim;
            _host = "127.0.0.1";

            Console.WriteLine($"Tcp using {_host}:{_port}");
        }

        /// <summary>
        /// Test tcp in command/response mode.
        /// </summary>
        public bool Run(CancellationTokenSource _ts)
        {
            bool err = false;

            while (!_ts.Token.IsCancellationRequested)
            {
                try
                {
                    //=========== Connect ============//
                    //https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener

                    using var server = TcpListener.Create(_port);
                    server.Start();

                    using var client = server.AcceptTcpClient(); // TODO1? AcceptTcpClientAsync(token)
                    Console.WriteLine("Client has connected");
                    using var stream = client.GetStream();


                    //=========== Receive ============//
                    string? cmd = null;
                    var rx = new byte[256]; // Max rx message for test.
                    var numRead = stream.Read(rx, 0, rx.Length); // blocks

                    if (numRead > 0)
                    {
                        for (int i = 0; i < numRead; i++)
                        {
                            if (rx[i] == _delim)
                            {
                                // Convert the received data to a string.
                                cmd = Encoding.Default.GetString(rx, 0, i);
                            }
                        }
                    }


                    //=========== Respond ============//
                    List<string>? response = null;

                    switch (cmd)
                    {
                        case null:
                            response = ["Bad delimiter (probably)"];
                            break;

                        case "l": // large payload - continuous
                            response = [.. File.ReadAllLines(@"C:\Dev\Apps\NTerm\Test\ross_2.txt")];
                            break;

                        case "s": // small payload
                            response = ["Everything's not great in life, but we can still find beauty in it."];
                            break;

                        case "e": // echo
                            response = [$"You sent [{cmd}]"];
                            break;

                        case "c": // ansi color
                            response = [$"Colors!!! \u001b[91mRED \u001b[92m GREEN \u001b[94mBLUE \u001b[0mNONE"];
                            break;

                        case "q":
                            response = ["Goodbye!"];
                            _ts.Cancel();
                            break;

                        default: // Always respond with something to prevent timeouts.
                            response = [$"Unknown cmd [{cmd}]"];
                            break;
                    }

                    Console.WriteLine($"cmd [{cmd}] response [{response[0]}]");

                    if (response is not null && response.Count > 0)
                    {
                        // Pace response messages. Simulates continuous operationn too.
                        int ind = 0;
                        while (!_ts.Token.IsCancellationRequested)
                        {
                            string send = response[ind];
                            byte[] bytes = [.. Encoding.Default.GetBytes(send), _delim];
                            stream.Write(bytes, 0, bytes.Length);
                            ind += 1;
                            if (ind >= response.Count)
                            {
                                //_ts.Cancel();
                                break;
                            }
                            else
                            {
                                // Pacing.
                                Thread.Sleep(ind % 10 == 0 ? 500 : 5);
                            }
                        }
                    }

                    // System.Threading.Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    // Log, reset, keep going.
                    Console.WriteLine($"Exception: {e}");
                    //server?.Stop();
                    // err = true;
                    // _ts.Cancel();
                }
            }

            return err;
        }
    }
}
