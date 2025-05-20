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
    // a test tcp server
    public class Server
    {
        public static void Run(int port)
        {
            Console.WriteLine($"Listening on {port}");
            var listener = TcpListener.Create(port);
            listener.Start();

            bool done = false;

            while (!done)
            {
                using var client = listener.AcceptTcpClient();
                Console.WriteLine("Client has connected");

                ////// Receive //////
                using var stream = client.GetStream();
                var rx = new byte[4096];
                Console.WriteLine("Start receive from client");
                var byteCount = stream.Read(rx, 0, rx.Length);

                if (byteCount > 0)
                {
                    var request = Utils.BytesToString(rx[..byteCount]);
                    Console.WriteLine($"Client said [{request}]");

                    ////// Reply /////
                    string response = "tx???";
                    switch (request)
                    {
                        case "L": // large payload
                            response = File.ReadAllText(@"C:\Dev\repos\Apps\NTerm\ross.txt");
                            break;

                        case "S": // small payload
                            response = "Everything's not great in life, but we can still find beauty in it.";
                            break;

                        case "E": // echo
                            response = $"You said [{request}]";
                            break;

                        case "C": // color
                            response = $"\033[91m red \033[92 green \033[94 blue \033[0m none";
                            break;

                        case "X":
                            done = true;
                            break;

                        default:
                            response = $"Unknown request: {request}";
                            break;
                    }

                    byte[] bytes = Utils.StringToBytes(response);

                    stream.Write(bytes, 0, bytes.Length);
                    Console.WriteLine($"Response: [{response}]");
                }
                else
                {
                    System.Threading.Thread.Sleep(10);
                }
            }
        }
    }
}
