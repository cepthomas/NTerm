using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using NTerm;


namespace Test
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        void Go()
        {
            // TODO1 start NTerm with arg =
            // test.ini
            // tcp 127.0.0.1 59120/30
            // udp 127.0.0.1 59140
            // null
        }


        void DoTcpCmdResp()
        {
            int port = 59120;

            PrintLine(Cat.Internal, $"Tcp using port: {port}");

            using CancellationTokenSource ts = new();

            //=========== Connect ============//
            using var listener = TcpListener.Create(port);
            // listener.SendTimeout = RESPONSE_TIME;
            // listener.SendBufferSize = BUFFER_SIZE;
            listener.Start();
            using var client = listener.AcceptTcpClient();
            PrintLine(Cat.Internal, "Client has connected");
            using var stream = client.GetStream();

            while (!ts.Token.IsCancellationRequested)
            {
                try
                {
                    //=========== Receive ============//
                    var rx = new byte[4096];
                    var byteCount = stream.Read(rx, 0, rx.Length);
                    var request = BytesToString(rx[..byteCount], byteCount); // or? BytesToStringReadable
                    PrintLine(Cat.Internal, $"Client request [{request}]");


                    //=========== Send ===============//
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

                    byte[] bytes = StringToBytes(response + Environment.NewLine);
                    stream.Write(bytes, 0, bytes.Length);
                    PrintLine(Cat.Internal, $"Server response: [{response.Substring(0, Math.Min(32, response.Length))}]");

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

        void DoTcpContinuous()
        {
            int port = 59130;

            PrintLine(Cat.Internal, $"Tcp using port: {port}");

            using CancellationTokenSource ts = new();

            var lines = File.ReadLines(@"C:\Dev\Apps\NTerm\Test\ross_1.txt");
            int ind = 0;

            //=========== Connect ============//
            using var listener = TcpListener.Create(port);
            // listener.SendTimeout = RESPONSE_TIME;
            // listener.SendBufferSize = BUFFER_SIZE;
            listener.Start();
            using var client = listener.AcceptTcpClient();
            PrintLine(Cat.Internal, "Client has connected");
            using var stream = client.GetStream();

            while (!ts.Token.IsCancellationRequested && ind < lines.Count)
            {
                try
                {
                    //=========== Send ===============//
                    string send = lines[ind++];

                    byte[] bytes = StringToBytes(send + Environment.NewLine);
                    stream.Write(bytes, 0, bytes.Length);
                    PrintLine(Cat.Internal, $"Server send: [{send.Substring(0, Math.Min(32, send.Length))}]");

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



        void DoUdpContinuous()
        {
            int port = 59140;

            PrintLine(Cat.Internal, $"Udp using port: {port}");

            using CancellationTokenSource ts = new();

            var lines = File.ReadLines(@"C:\Dev\Apps\NTerm\Test\ross_2.txt");
            int ind = 0;

            //=========== Connect ============//
            UdpClient client = new UdpClient();
            client.Connect("127.0.0.1", port);
            PrintLine(Cat.Internal, "Client has connected");

            while (!ts.Token.IsCancellationRequested && ind < lines.Count)
            {
                try
                {
                    //=========== Send ===============//
                    string send = lines[ind++];

                    byte[] bytes = StringToBytes(send + Environment.NewLine);

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




        void PrintLine(Cat cat, string msg)
        {
            var scat = cat switch
            {
                Cat.Send => ">>>",
                Cat.Receive => "<<<",
                Cat.Error => "!!!",
                Cat.Internal => "---",
                _ => throw new NotImplementedException(),
            };
            
            var s = $"{scat} {text}{Environment.NewLine}";

            TxtDisplay.AppendText(s);
        }





        public string BytesToString(byte[] buff, int cnt)
        {
            return Encoding.Default.GetString(buff, 0, cnt);
        }

        public string BytesToStringReadable(byte[] buff, int cnt)
        {
            List<string> list = [];
            for (int i = 0; i < cnt; i++)
            {
                var c = buff[i];
                list.Add(c.IsReadable() ? ((char)c).ToString() : $"<{c:X}>");
            }
            return string.Join("", list);
        }

        public byte[] StringToBytes(string s)
        {
            // Valid strings are always convertible.
            return Encoding.Default.GetBytes(s);
        }
    }
}
