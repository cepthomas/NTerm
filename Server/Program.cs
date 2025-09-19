using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TcpListener server = null;

            try
            {
                // Set the IP address and port for the server to listen on
                IPAddress localAddr = IPAddress.Parse("127.0.0.1"); // Or IPAddress.Any for any available IP
                int port = 59120;

                // Create a TcpListener object
                server = new TcpListener(localAddr, port);

                // Start listening for client requests
                server.Start();
                Console.WriteLine("TCP Server started. Listening on {0}:{1}", localAddr, port);

                // Enter the listening loop
                while (true)
                {
                    Console.WriteLine("Waiting for a client connection...");

                    // Accept a pending client connection
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Client connected!");

                    // Get a network stream for reading and writing
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[256];
                    int bytesRead;

                    // Loop to receive all the data sent by the client
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        // Convert the received data to a string
                        string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine("Received: {0}", data);

                        // Echo the data back to the client
                        byte[] msg = Encoding.UTF8.GetBytes("Server received: " + data);
                        stream.Write(msg, 0, msg.Length);
                        Console.WriteLine("Sent: Server received: {0}", data);
                    }

                    // Close the client connection
                    client.Close();
                    Console.WriteLine("Client disconnected.");
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new client requests
                server?.Stop();
            }

            Console.WriteLine("\nPress Enter to continue...");
            Console.Read();
        }
    }
}
