using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace TestX
{
    public class TcpServer
    {
        public enum Cat { None, Send, Receive, Error, Info }

        /// <summary>
        /// Show the user.
        /// </summary>
        /// <param name="cat"></param>
        /// <param name="text"></param>
        /// <exception cref="NotImplementedException"></exception>
        void PrintLine(Cat cat, string text)
        {
            var scat = cat switch
            {
                Cat.Send => ">>>",
                Cat.Receive => "<<<",
                Cat.Error => "!!!",
                Cat.Info => "---",
                _ => throw new NotImplementedException(),
            };

            var s = $"{scat} {text}{Environment.NewLine}";

            Console.Write(s);
        }









        ///// From online example. /////
        public void BasicExample(List<string> args)
        {
            TcpListener server = null;
            byte _delim = 10; // LF

            try
            {
                // Set the IP address and port for the server to listen on
                IPAddress localAddr = IPAddress.Parse("127.0.0.1"); // Or IPAddress.Any for any available IP
                int port = 59120;

                // Create a TcpListener object
                server = new TcpListener(localAddr, port);

                // Start listening for client requests
                server.Start();
                Console.WriteLine($"TCP Server started. Listening on {localAddr}:{port}");

                // Enter the listening loop. TODO1 forever.
                while (true)
                {
                    Console.WriteLine("Waiting for a client connection...");

                    // Accept a pending client connection
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Client connected!");

                    // Get a network stream for reading and writing
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[256];

                    // Loop to receive all the data sent by the client
                    bool done = false;

                    while (!done)
                    {
                        var numRead = stream.Read(buffer, 0, buffer.Length);

                        if (numRead > 0) // TODO1 look for delim and remove it!
                        {
                            // Convert the received data to a string
                            string data = Encoding.UTF8.GetString(buffer, 0, numRead);
                            Console.WriteLine($"Received: {data}");

                            // Echo the data back to the client
                            var secho = $"GOT {data}";
                            Console.WriteLine($"Replying: {secho}");

                            byte[] msg = Encoding.Default.GetBytes(secho).Append(_delim).ToArray();
                            stream.Write(msg, 0, msg.Length);
                            Console.WriteLine($"Reply done");

                            done = true;
                        }
                        else
                        {
                            Console.WriteLine($"GOT nada");
                            done = true;
                        }
                    }

                    // Close the client connection
                    client.Close();
                    Console.WriteLine("Client disconnected.");
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Other Exception: {e}");
            }
            finally
            {
                // Stop listening for new client requests
                server?.Stop();
            }

            Console.WriteLine("Press Enter to continue...");
            Console.Read();
        }




/////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Test tcp in command/response mode.
        /// </summary>
        public void DoTcpCmdResp()
        {
            // Start server.
            int port = 59120;

            PrintLine(Cat.Info, $"Tcp using port: {port}");

            //Go(cfile);


            using CancellationTokenSource ts = new();


            while (!ts.Token.IsCancellationRequested)
            {
                try
                {
                    //=========== Connect ============//
                    //https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener

                    using var server = TcpListener.Create(port);
                    // listener.SendTimeout = RESPONSE_TIME;
                    // listener.SendBufferSize = BUFFER_SIZE;
                    server.Start();


                    using var client = server.AcceptTcpClient();

                    PrintLine(Cat.Info, "Client has connected");

                    using var stream = client.GetStream();


                    //=========== Receive ============//
                    var rx = new byte[4096];
                    var byteCount = stream.Read(rx, 0, rx.Length);
                    var request = Encoding.Default.GetString(rx[..byteCount], 0, byteCount); // or? BytesToStringReadable
                    PrintLine(Cat.Info, $"Client request [{request}]");


                    //=========== Send ===============//
                    string response = "";
                    switch (request)
                    {
                        case "l": // large payload
                            response = File.ReadAllText(@"C:\Dev\Apps\NTerm\Test\ross_1.txt");
                            break;

                        case "s": // small payload
                            response = "Everything's not great in life, but we can still find beauty in it.";
                            break;

                        case "e": // echo
                            response = $"You said [{request}]";
                            break;

                        case "c": // ansi color
                            response = $"\u001b[91mRED \u001b[92m GREEN \u001b[94mBLUE \u001b[0mNONE";
                            break;

                        case "x":
                            ts.Cancel();
                            break;

                        default: // Always respond with something to prevent timeouts.
                            response = $"Unknown request: {request}";
                            break;
                    }

                    byte[] bytes = Encoding.Default.GetBytes(response + Environment.NewLine);
                    stream.Write(bytes, 0, bytes.Length);
                    PrintLine(Cat.Info, $"Server response: [{response.Substring(0, Math.Min(32, response.Length))}]");

                    // System.Threading.Thread.Sleep(10);
                }
                catch (Exception e)
                {
                    PrintLine(Cat.Error, $"Exception: {e}");
                    ts.Cancel();
                }
                // catch (SocketException e)
                // catch (IOException e)
            }
        }

        /// <summary>
        /// Test tcp in continuous mode.
        /// </summary>
        public void DoTcpContinuous()
        {
            //// Tweak config.
            //var config = BuildConfig("tcp 127.0.0.1 59130");
            //File.WriteAllLines(cfile, config);
            //Go(cfile);


            // Start server.
            int port = 59130;

            PrintLine(Cat.Info, $"Tcp using port: {port}");

            using CancellationTokenSource ts = new();

            var lines = File.ReadAllLines(@"C:\Dev\Apps\NTerm\Test\ross_1.txt");
            int ind = 0;

            //=========== Connect ============//
            using var listener = TcpListener.Create(port);
            // listener.SendTimeout = RESPONSE_TIME;
            // listener.SendBufferSize = BUFFER_SIZE;
            listener.Start();
            using var client = listener.AcceptTcpClient();
            PrintLine(Cat.Info, "Client has connected");
            using var stream = client.GetStream();

            while (!ts.Token.IsCancellationRequested && ind < lines.Length)
            {
                try
                {
                    //=========== Send ===============//
                    string send = lines[ind++];

                    byte[] bytes = Encoding.Default.GetBytes(send + Environment.NewLine);
                    stream.Write(bytes, 0, bytes.Length);
                    PrintLine(Cat.Info, $"Server send: [{send.Substring(0, Math.Min(32, send.Length))}]");

                    // Pacing.
                    System.Threading.Thread.Sleep(500);
                }
                catch (Exception e)
                {
                    PrintLine(Cat.Error, $"Exception: {e}");
                    ts.Cancel();
                }
                // catch (SocketException e)
                // catch (IOException e)
            }
        }







    }
}
