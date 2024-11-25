using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace NTerm.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Invalid args");
                Environment.Exit(1);
            }

            int port = int.Parse(args[0]); // 59120;
            Console.WriteLine($"Listening on {port}");

            bool done = false;

            while (!done)
            {
                var listener = TcpListener.Create(port);
                listener.Start();

                using var client = listener.AcceptTcpClient();
                Console.WriteLine("Client has connected");

                ////// Receive //////
                using var stream = client.GetStream();
                var buffer = new byte[4096];
                Console.WriteLine("Start receive from client");
                var byteCount = stream.Read(buffer, 0, buffer.Length);
                var request = Encoding.UTF8.GetString(buffer, 0, byteCount);
                Console.WriteLine($"Client said [{request}]");

                ////// Reply /////
                string resp = "???";
                switch (request)
                {
                    case "l": // large payload
                        resp = File.ReadAllText(@"C:\Dev\repos\Apps\NTerm\ross.txt");
                        break;

                    case "s": // small payload
                        resp = "Everything's not great in life, but we can still find beauty in it.";
                        break;

                    case "e": // echo
                        resp = $"You said [{request}]";
                        break;

                    case "c": // color
                        resp = $"\033[91m red \033[92 green \033[94 blue \033[0m none";
                        break;

                    case "x":
                        done = true;
                        break;
                }

                byte[] bytes = Encoding.UTF8.GetBytes(resp);

                stream.Write(bytes, 0, bytes.Length);
                Console.WriteLine($"Response {resp[..100]}");
            }
        }
    }
}
