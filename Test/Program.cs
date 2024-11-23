using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace Server
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            int port = 59120;
            bool done = false;

            while (!done)
            {
                var listener = TcpListener.Create(port);
                listener.Start();
                Console.WriteLine($"[Server] Try {port} {listener.LocalEndpoint}");

                using var client = listener.AcceptTcpClient();
                Console.WriteLine("[Server] Client has connected");

                ////// Receive //////
                using var stream = client.GetStream();
                var buffer = new byte[4096]; // TODO maybe handle larger payload.
                Console.WriteLine("[Server] Start receive from client");
                var byteCount = stream.Read(buffer, 0, buffer.Length);
                var request = Encoding.UTF8.GetString(buffer, 0, byteCount);
                Console.WriteLine($"[Server] Client said [{request}]");

                done = request == "x";

                ////// Reply /////
                string resp = File.ReadAllText(@"C:\Dev\repos\Apps\NTerm\ross.txt");
                byte[] bytes = Encoding.UTF8.GetBytes(resp);

                stream.Write(bytes, 0, bytes.Length);
                Console.WriteLine($"[Server] Response {resp.Substring(0, 50)}");
            }
        }
    }
}
