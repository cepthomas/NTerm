using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace TestX
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TcpServer srv = new();

            srv.BasicExample([]);

            return;


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

                        if (numRead > 0) // TODO1 and/or look for delim
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
    }
}
