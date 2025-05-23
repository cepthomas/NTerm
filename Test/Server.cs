using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using NTerm;

        // [DisplayName("Prompt")]
        // [Description("CLI prompt.")]
        // [Category("NTerm")]
        // [Browsable(true)]

namespace Test
{
    public class Server
    {
        public static void Run(int port)
        {
            bool done = false;

            while (!done)
            {
                try
                {
                    //Console.WriteLine($"Listening on {port}");
                    using var listener = TcpListener.Create(port);

                    listener.Start();

                    using var client = listener.AcceptTcpClient();
                    //Console.WriteLine("Client has connected");

                    ////// Receive //////
                    using var stream = client.GetStream();
                    var rx = new byte[4096];
                    //Console.WriteLine("Start receive from client");
                    var byteCount = stream.Read(rx, 0, rx.Length);

                    if (byteCount > 0)
                    {
                        var request = Utils.BytesToString(rx[..byteCount]);
                        Console.WriteLine($"Client request [{request}]");

                        ////// Reply /////
                        string response = "";
                        switch (request)
                        {
                            case "l": // large payload
                                response = File.ReadAllText(@"C:\Dev\Apps\NTerm\Test\ross.txt");
                                break;

                            case "s": // small payload
                                response = "Everything's not great in life, but we can still find beauty in it.";
                                break;

                            case "e": // echo
                                response = $"You said [{request}]";
                                break;

                            case "c": // color
                                response = $"\033[91m red \033[92 green \033[94 blue \033[0m none";
                                break;

                            // case "x":
                            //     done = true;
                            //     break;

                            default: // Always respond with something to prevent timeouts.
                                response = $"Unknown request: {request}";
                                break;
                        }

                        // byte[] bytes = Utils.StringToBytes($"{response}{Environment.NewLine}{_prompt}");
                        byte[] bytes = Utils.StringToBytes(response);
                        stream.Write(bytes, 0, bytes.Length);
                        Console.WriteLine($"Server response: [{response.Substring(0, Math.Min(32, response.Length))}]");
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
                catch (IOException e)
                {
                    Console.WriteLine("IOException: {0}", e);
                }
                catch (Exception e)
                {
                    Console.WriteLine("!!! Other Exception: {0}", e);
                    done = true;
                }

                //Other Exception: System.IO.IOException: Unable to read data from the transport connection: An existing connection was forcibly closed by the remote host..
                // ---> System.Net.Sockets.SocketException (10054): An existing connection was forcibly closed by the remote host.
                //   at System.Net.Sockets.NetworkStream.Read(Byte[] buffer, Int32 offset, Int32 count)
                //   --- End of inner exception stack trace ---
                //   at System.Net.Sockets.NetworkStream.Read(Byte[] buffer, Int32 offset, Int32 count)
                //   at Test.Server.Run(Int32 port) in C:\Dev\Apps\NTerm\Test\Server.cs:line 37
            }
        }
    }
}
