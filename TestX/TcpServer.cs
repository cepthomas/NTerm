using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Ephemera.NBagOfTricks;
using NTerm;


namespace TestX
{
    public class TcpServer
    {
        int _port = 0; // 59120
        byte _delim = 10; // LF
        CancellationTokenSource _ts;

        // const int CONNECT_TIME = 50;
        // const int RESPONSE_TIME = 1000;
        // const int BUFFER_SIZE = 4096;

        public TcpServer(int port, byte delim, CancellationTokenSource ts)
        {
            _port = port;
            _delim = delim;
            _ts = ts;

            PrintLine(Cat.Info, $"Tcp using port: {_port}");
        }

        /// <summary>
        /// Test tcp in command/response mode.
        /// </summary>
        public bool Run()
        {
            bool err = false;

            while (!_ts.Token.IsCancellationRequested)
            {
                try
                {
                    //=========== Connect ============//
                    using var server = TcpListener.Create(_port);
                    // listener.SendTimeout = RESPONSE_TIME;
                    // listener.SendBufferSize = BUFFER_SIZE;
                    server.Start();


                    using var client = server.AcceptTcpClient();
                    PrintLine(Cat.Info, "Client has connected");
                    using var stream = client.GetStream();


                    //=========== Receive ============//
                    string? cmd = null;
                    var rx = new byte[256]; // Fixed max message. Really should buffer.
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
                    string response = "";
                    switch (cmd)
                    {
                        case "l": // large payload  TODO1 continuous
                            response = File.ReadAllText(@"C:\Dev\Apps\NTerm\Test\ross_1.txt");
                            break;

                        case "s": // small payload
                            response = "Everything's not great in life, but we can still find beauty in it.";
                            break;

                        case "e": // echo
                            response = $"You said [{cmd}]";
                            break;

                        case "c": // ansi color
                            response = $"\u001b[91mRED \u001b[92m GREEN \u001b[94mBLUE \u001b[0mNONE";
                            break;

                        case "q":
                            ts.Cancel();
                            break;

                        case null:
                            response = $"Got a null";
                            break;

                        default: // Always respond with something to prevent timeouts.
                            response = $"Unknown cmd: {cmd}";
                            break;
                    }

                    byte[] bytes = Encoding.Default.GetBytes(response).Append(_delim).ToArray();
                    stream.Write(bytes, 0, bytes.Length);
                    PrintLine(Cat.Info, $"Response: [{response.Substring(0, Math.Min(32, response.Length))}]");

                    // System.Threading.Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    PrintLine(Cat.Error, $"Exception: {e}");
                    err = true;
                    ts.Cancel();

                    // case SocketException ex: // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                    //     int[] valid = [10053, 10054, 10060, 10061, 10064];
                    //     if (valid.Contains(ex.NativeErrorCode))
                    //     {
                    //         // Ignore and retry later.
                    //     }
                    //     else
                    //     {
                    //         // All other errors are considered fatal - bubble up to App to handle.
                    //         throw;
                    //     }
                    //     break;

                    // case OperationCanceledException: // Usually connect timeout. Ignore and retry later.
                    //     break;

                    // case IOException: // Usually receive timeout. Ignore and retry later.
                    //     break;

                    // default: // All other errors are considered fatal - bubble up to App to handle.
                    //     throw;
                }

                finally
                {
                    // Stop listening for this iteration.
                    _server?.Stop();
                }
            }

            return err;
        }
    }
}
