using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Ephemera.NBagOfTricks;




// Works:
//AsyncUsingTcpClient();
//StartServer(_config.Port);
//var res = _comm.Send("djkdjsdfksdf;s");
//Write($"{res}: {_comm.Response}");
//var ser = new Serial();
//var ports = ser.GetSerialPorts();


///// <summary>
///// 
///// </summary>
///// <param name="sender"></param>
///// <param name="e"></param>
//void BtnGo_Click(object? sender, EventArgs e)
//{
//    try
//    {
//        // Works:
//        //AsyncUsingTcpClient();

//        //StartServer(_config.Port);
//        //var res = _prot.Send("djkdjsdfksdf;s");
//        //tvOut.AppendLine(res);

//    }
//    catch (Exception ex)
//    {
//        _logger.Error($"Fatal error: {ex.Message}");
//    }
//}




namespace NTerm
{
    // a test tcp server
    public class DebugServer
    {
        public static void Run(int port)
        {
            Console.WriteLine($"Listening on {port}");

            //// Start test server.
            //string uri = @"C:\Dev\repos\Apps\NTerm\bin\Debug\net8.0-windows\NTerm.exe";
            //var info = new ProcessStartInfo(uri) { UseShellExecute = true };
            //var proc = new Process() { StartInfo = info };
            //proc.Start();

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
                string resp = "resp???";
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
