using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using NTerm;


namespace Test
{
    public partial class MainForm : Form
    {
        // TODO1 run using explicit cl args.

        #region Fields
        readonly string me = @"C:\Dev\Apps\NTerm\Test\bin\net8.0-windows\Test.exe";
        readonly string exe = @"C:\Dev\Apps\NTerm\bin\net8.0-windows\win-x64\NTerm.exe";
        readonly string cfile = @"C:\Dev\Apps\NTerm\Test\test.ini";
        readonly ConsoleColorEx clr = ConsoleColorEx.None;
        CancellationTokenSource ts = new();
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            //BtnGo.Click += (_, __) => DoAsync();
            BtnGo.Click += (_, __) => DoTcpCmdResp();
        }


        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        //  https://stackoverflow.com/a/53403824   c# 7.0 in a nutshell
        const int packet_length = 2;  // user defined packet length

        void DoAsync()
        {
            // Tweak config.
            var config = BuildConfig("tcp 127.0.0.1 59120");
            File.WriteAllLines(cfile, config);

            RunServerAsync();

            Go(cfile);
        }

        async void RunServerAsync()
        {
            var listner = new TcpListener(IPAddress.Any, 59120);
            listner.Start();
            try
            {
                while (true)
                {
                    // was await Accept(await listner.AcceptTcpClientAsync());
                    TcpClient client = await listner.AcceptTcpClientAsync();
                    await Accept(client);
                }
            }
            finally
            {
                listner.Stop();
            }
        }

        async Task Accept(TcpClient client)
        {
            await Task.Yield();
            try
            {
                using (client)
                using (NetworkStream n = client.GetStream())
                {
                    byte[] data = new byte[packet_length];
                    int bytesRead = 0;
                    int chunkSize = 1;

                    while (bytesRead < data.Length && chunkSize > 0)
                    {
                        bytesRead += chunkSize = await n.ReadAsync(data, bytesRead, data.Length - bytesRead);
                    }

                    // get data
                    string str = Encoding.Default.GetString(data);
                    Console.WriteLine("[server] received : {0}", str);

                    // To do
                    // ...

                    // send the result to client
                    string send_str = "server_send_test";
                    byte[] send_data = Encoding.ASCII.GetBytes(send_str);
                    await n.WriteAsync(send_data, 0, send_data.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////




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
            // Tweak config.
            var config = BuildConfig("null");
            File.WriteAllLines(cfile, config);

            Go(cfile);
        }

        /// <summary>
        /// Test tcp in command/response mode.
        /// </summary>
        void DoTcpCmdResp()
        {
            // Tweak config.
            var config = BuildConfig("tcp 127.0.0.1 59120");
            File.WriteAllLines(cfile, config);

            // Start server.
            int port = 59120;

            PrintLine(Cat.Info, $"Tcp using port: {port}");

            Go(cfile);


            using CancellationTokenSource tsxxx = new();


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
        void DoTcpContinuous()
        {
            // Tweak config.
            var config = BuildConfig("tcp 127.0.0.1 59130");
            File.WriteAllLines(cfile, config);


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

        /// <summary>
        /// Test udp in continuous mode.
        /// </summary>
        void DoUdpContinuous()
        {
            // Tweak config.
            var config = BuildConfig("udp 127.0.0.1 59140");
            File.WriteAllLines(cfile, config);

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


        /// <summary>
        /// Clone and mod the default config.
        /// </summary>
        /// <param name="commType"></param>
        /// <returns></returns>
        List<string> BuildConfig(string commType)
        {
            List<string> defaultConfig = [
                "[nterm]",
                //"comm_type = null",
                "delim = LF",
                "prompt = >",
                "meta = -",
                "info_color = darkcyan",
                "err_color = green",
                "[macros]",
                "dox = \"do xxxxxxx\"",
                "s3 = \"hey, send 333333333\"",
                "tm = \"  xmagentax   -yellow-  \"",
                "[matchers]",
                "\"mag\" = magenta",
                "\"yel\" = yellow"];

            // Clone and mod defaultConfig => comm_type = null
            List<string> config = [];
            defaultConfig.ForEach(l =>
            {
                config.Add(new(l));
                if (l.Contains("[nterm]"))
                {
                    config.Add($"comm_type = {commType}");
                }
            });

            return config;
        }


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

            TxtDisplay.AppendText(s);
        }
    }
}
