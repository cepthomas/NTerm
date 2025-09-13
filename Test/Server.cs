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


namespace Test
{
    public class Server // TODO1 need cmd/resp TCP, cont TCP, cont UDP(client)
    {
        public static void Run(int port)
        {
            bool done = false;
            Console.WriteLine($"Listening on {port}");

            while (!done)
            {
                try
                {
                    //=========== Connect ============//
                    using var listener = TcpListener.Create(port);
                    listener.Start();
                    using var client = listener.AcceptTcpClient();
                    Console.WriteLine("Client has connected");

                    //=========== Receive ============//
                    using var stream = client.GetStream();
                    var rx = new byte[4096];
                    //Console.WriteLine("Start receive from client");
                    var byteCount = stream.Read(rx, 0, rx.Length);
                    var request = Utils.BytesToString(rx[..byteCount], byteCount);
                    Console.WriteLine($"Client request [{request}]");

                    //=========== Respond ============//
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

                        case "c":
                            response = $"\u001b[91mRED \u001b[92m GREEN \u001b[94mBLUE \u001b[0mNONE";
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

                    // System.Threading.Thread.Sleep(10);
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
            }
        }
    }
}
