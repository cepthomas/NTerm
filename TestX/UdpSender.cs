using System;
//using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
//using Ephemera.NBagOfTricks;
//using Ephemera.NBagOfTricks.PNUT;
//using NTerm;


namespace TestX
{
    public class UdpSender
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



        // Create a server on the Academic RIO Device that listens for UDP datagram messages from a client
        // running on the PC host, accepts client information including the desired state of the four onboard LEDs,
        // sets the LEDs accordingly


        /// <summary>
        /// Test udp in continuous mode.
        /// </summary>
        void DoUdpContinuous()
        {
            // Start client.
            int port = 59140;

            PrintLine(Cat.Info, $"Udp using port: {port}");

            using CancellationTokenSource ts = new();

            var lines = File.ReadAllLines(@"C:\Dev\Apps\NTerm\Test\ross_2.txt");
            int ind = 0;

            //=========== Connect ============//
            UdpClient client = new UdpClient();

            client.Connect("127.0.0.1", port);
            PrintLine(Cat.Info, "Client has connected");

            while (!ts.Token.IsCancellationRequested && ind < lines.Length)
            {
                try
                {
                    //=========== Send ===============//
                    string send = lines[ind++];

                    byte[] bytes = Encoding.Default.GetBytes(send + Environment.NewLine);

                    client.Send(bytes, bytes.Length);

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
