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
using System.Threading;


namespace Test
{
    public partial class MainForm : Form
    {
        #region Fields

        readonly List<string> defaultConfig = [
        "[nterm]",
        "comm_type = null",
        "delim = LF",
        "prompt = >",
        "meta = -",
        "info_color = darkcyan",
        "err_color = green",
        "[macros]",
        "dox = \"do xxxxxxx\"",
        "s3 = \"send 333333333\"",
        "tm = \"  xmagx   -yel-  \"",
        "[matchers]",
        "\"mag\" = magenta",
        "\"yel\" = yellow"];


        readonly string me = @"C:\Dev\Apps\NTerm\Test\bin\net8.0-windows\Test.exe";
        readonly string exe = @"C:\Dev\Apps\NTerm\bin\net8.0-windows\win-x64\NTerm.exe";
        readonly string cfile = @"C:\Dev\Apps\NTerm\Test\test.ini";

        // colors: black darkblue darkgreen darkcyan darkred darkmagenta darkyellow gray darkgray blue green cyan red magenta yellow white

        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            BtnGo.Click += (_, __) => DoConfig();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
        }
        #endregion

        /// <summary>
        /// Simple first test.
        /// </summary>
        void DoBasic()
        {
            Go("null");
        }

        /// <summary>
        /// Test config functions.
        /// </summary>
        void DoConfig()
        {
            // Copy and mod defaultConfig => comm_type = null
            // Write to test.ini.

            // readonly List<string> defaultConfig = [
            List<string> config = [];

            defaultConfig.ForEach(l =>
            {
                if (l.StartsWith("comm_type"))
                {
                    config.Add("comm_type = null");
                }
                else
                {
                    config.Add(l);
                }
            });

            File.WriteAllLines(cfile, config);

            Go(cfile);

        }

        void DoTcpCmdResp()
        {
            ///// Test tcp in command/response mode. /////

            // Copy and mod defaultConfig => comm_type = tcp 127.0.0.1 59120  ; C/R
            // Write to test.ini.

            Go(cfile);



            // Start server.

            int port = 59120;

            PrintLine(Cat.Info, $"Tcp using port: {port}");

            using CancellationTokenSource ts = new();

            //=========== Connect ============//
            using var listener = TcpListener.Create(port);
            // listener.SendTimeout = RESPONSE_TIME;
            // listener.SendBufferSize = BUFFER_SIZE;
            listener.Start();
            using var client = listener.AcceptTcpClient();
            PrintLine(Cat.Info, "Client has connected");
            using var stream = client.GetStream();

            while (!ts.Token.IsCancellationRequested)
            {
                try
                {
                    //=========== Receive ============//
                    var rx = new byte[4096];
                    var byteCount = stream.Read(rx, 0, rx.Length);
                    var request = BytesToString(rx[..byteCount], byteCount); // or? BytesToStringReadable
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

                    byte[] bytes = StringToBytes(response + Environment.NewLine);
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

        void DoTcpContinuous()
        {
            ///// Test tcp in continuous mode. /////

            // Copy and mod defaultConfig => comm_type = tcp 127.0.0.1 59130 ; cont
            // Write to test.ini.

            Go(cfile);



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

                    byte[] bytes = StringToBytes(send + Environment.NewLine);
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



        void DoUdpContinuous()
        {
            ///// Test udp in continuous mode. /////


            // Copy and mod defaultConfig => comm_type = udp 127.0.0.1 59140
            // Write to test.ini.

            Go(cfile);



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


        /// <summary>
        /// Run the exe with full user cli.
        /// </summary>
        /// <param name="args"></param>
        void Go(string args)
        {

            ProcessStartInfo pinfo = new(exe, args)
            {
            };

            using Process proc = new() { StartInfo = pinfo };

            PrintLine(Cat.Info, "Start process...");
            proc.Start();

            PrintLine(Cat.Info, "Wait for exit...");
            proc.WaitForExit();

            PrintLine(Cat.Info, "Exited...");
        }


        /// <summary>
        /// Run the exe but take ownership of the user cli.
        /// </summary>
        /// <param name="args"></param>
        void GoSteal(string args)
        {

            ProcessStartInfo pinfo = new(exe, args)
            {
                UseShellExecute = false,
                //CreateNoWindow = true,
                //WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // Add app-specific environmental variables.
            // pinfo.EnvironmentVariables["MY_VAR"] = "Hello!";

            using Process proc = new() { StartInfo = pinfo };
            //proc.Exited += (sender, e) => { LogInfo("Process exit event."); };

            PrintLine(Cat.Info, "Start process...");
            proc.Start();

            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();

            //LogInfo("Wait for process to exit...");
            proc.WaitForExit();


            PrintLine(Cat.Info, "Exited...");
            // return new(proc.ExitCode, stdout, stderr);
        }





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
