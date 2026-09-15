using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using Ephemera.NBagOfTricks;


// TODO1 copy these:
// C:\Dev\Apps\NTerm-new
// |   App.cs (14k)
// |   Common.cs (7k)
// |   Config.cs (5k)
// |   IConsole.cs (7k)
// |   NullComm.cs (1k)
// |   Program.cs (410b)
// |   README.md (2k)
// |   SerialComm.cs (5k)
// |   TcpComm.cs (9k)
// |   UdpComm.cs (5k)
// \---Test
//         tcp_server.py (1k)
//         test_config_null.ini (54b)
//         test_config_tcp.ini (218b)
//         test_config_udp.ini (220b)
//         udp_sender.py (2k)


namespace NTerm
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>Current config.</summary>
        readonly Config _config = new();

        /// <summary>Client comm flavor.</summary>
        readonly IComm _comm = new NullComm();

        /// <summary>The user console.</summary>
        readonly IConsole _console;

        /// <summary>Module logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("NTerm");
        #endregion

        #region Lifecycle
        /// <summary>
        /// Build me one and make it go.
        /// <param name="args">Command line.</param>
        /// <param name="console">Console to use. Can override default for testing.</param>
        /// </summary>
        public App(List<string> args, IConsole? console = null)
        {
            int exitCode = 0;
            string serror = "";
            _console = console ?? new RealConsole();

            //Dev();

            try
            {
                string appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");

                ///// Init logging. Hard-coded log levels. /////
                string logFileName = Path.Combine(appDir, "log.txt");
                LogManager.MinLevelFile = LogLevel.Trace;
                LogManager.MinLevelNotif = LogLevel.Info;
                LogManager.LogMessage += LogMessage;
                LogManager.Run(logFileName, 100000);

                if (args.Count == 0)
                {
                    Usage(true);
                    Environment.Exit(1);
                }

                ///// Load config. /////
                // Is there a default ini? if not, make one.
                string defaultConfig = Path.Combine(appDir, "default.ini");
                if (!File.Exists(defaultConfig))
                {
                    string scfig = """
                        ; User defaults - overriden by specific ini.
                        [nterm]
                        comm = null
                        error_color = red
                        [macros]
                        ; none
                        [matchers]
                        ; none
                        """;
                    File.WriteAllText(defaultConfig, scfig);
                    _logger.Info($"Created default config {defaultConfig} - edit to taste.");
                }

                _config = new();
                _config.Load(args, defaultConfig);

                ///// Process comm config. /////
                _comm = _config.CommConfig[0].ToLower() switch
                {
                    "null" => new NullComm(),
                    "tcp" => new TcpComm(_config.CommConfig),
                    "udp" => new UdpComm(_config.CommConfig),
                    "serial" => new SerialComm(_config.CommConfig),
                    _ => throw new ConfigException($"Invalid comm type: [{_config.CommConfig[0]}]"),
                };

                _logger.Info($"NTerm using {_comm}");

                ///// Go! /////
                using CancellationTokenSource ts = new();

                // Hook exit key.
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    ts.Cancel();
                };

                RunForever(ts);

                _logger.Info($"NTerm exit");
            }
            // Any exception that arrives here is considered fatal. Inform and exit.
            catch (ConfigException ex) // known ini error
            {
                serror = $"{ex.Message}";
                _logger.Error(serror);
                exitCode = 1;
            }
            catch (IniSyntaxException ex) // known ini error
            {
                serror = $"Ini syntax error at line {ex.LineNum}: {ex.Message}";
                _logger.Error(serror);
                exitCode = 1;
            }
            catch (Exception ex) // other/unexpected error
            {
                serror = ex.Message;
                _logger.Exception(ex);
                exitCode = 1;
            }

            if (serror.Length > 0)
            {
                MessageBox.Show(serror, "Error! - see  log");
            }

            LogManager.Stop();

            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            _comm?.Dispose();
        }
        #endregion

        #region Main Loop
        /// <summary>
        /// Main loop.
        /// </summary>
        public async void RunForever(CancellationTokenSource ts)
        {
            try
            {
                // Hook up progress reporting.
                var rxHandler = new Progress<byte[]>(value => { ProcessReceive(value); });
                var kbdHandler = new Progress<string>(value => { if (ProcessUserInput(value)) ts.Cancel(); });

                // Fire off multiple long-running async background operations
                // TIL: Don't call explicit Dispose() on tasks. That includes using statements.
                // https://devblogs.microsoft.com/dotnet/do-i-need-to-dispose-of-tasks/
                Task taskKeyboard = RunConsole(ts.Token, kbdHandler);
                Task taskComm = _comm.Run(ts.Token, rxHandler);

                // These are forever tasks. If any stops it indicates shutdown - normal or error.
                await Task.WhenAny(taskKeyboard, taskComm);

                // Check for task errors (and/or Status?) and do something with them.
                if (taskKeyboard.Exception is not null && taskKeyboard.Exception.InnerException is not null)
                {
                    _logger.Exception(taskKeyboard.Exception.InnerException);
                }
                if (taskComm.Exception is not null && taskComm.Exception.InnerException is not null)
                {
                    _logger.Exception(taskComm.Exception.InnerException);
                }
            }
            catch (TaskCanceledException ex)
            {
                _logger.Debug($"Normal TaskCanceledException [{ex.Message}]");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
                Tell(ex.Message, ConsoleColor.Red);
            }
        }
        #endregion

        #region Process inputs
        /// <summary>
        /// The keyboard input task.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public async Task RunConsole(CancellationToken token, IProgress<string> progress)
        {
            bool done = false;

            while (!done && !token.IsCancellationRequested)
            {
                //token.ThrowIfCancellationRequested();

                // Check for something to do.
                if (_console.KeyAvailable)
                {
                    var cl = _console.ReadLine(); // blocks
                    if (cl is not null && cl.Length > 0)
                    {
                        progress.Report(cl);
                    }
                }

                // Relax a bit.
                await Task.Delay(10, token);
            }
        }


        ///// <summary>
        ///// Task to service the user input read.
        ///// </summary>
        ///// <param name="token"></param>
        //void DoKeyboard(CancellationToken token)
        //{
        //    while (!token.IsCancellationRequested)
        //    {
        //        var kbdin = "";

        //        // Check for something to do.
        //        if (_console.KeyAvailable)
        //        {
        //            var k = _console.ReadKey();
        //            kbdin += k.KeyChar;

        //            if (k.Key == ConsoleKey.Escape)
        //            {
        //                // Meta command. Get the next char.
        //                kbdin += _console.ReadKey().KeyChar;
        //                _qUserCli.Enqueue(new(kbdin));
        //            }
        //            else
        //            {
        //                // Terminal command.
        //                var s = _console.ReadLine();
        //                if (s is not null && s.Length != 0)
        //                {
        //                    _qUserCli.Enqueue(kbdin + s);
        //                }
        //            }
        //        }

        //        // Don't be greedy.
        //        Thread.Sleep(50);
        //    }
        //}










        /// <summary>
        /// Process user entry. Always line oriented.
        /// </summary>
        /// <param name="sin"></param>
        /// <returns>True if quit</returns>
        bool ProcessUserInput(string sin)
        {
            bool quit = false;

            // May be meta command.
            var first = sin[0];
            var rest = sin[1..];

            // Interpret the input.
            if (first == _config.MetaInd)
            {
                switch (rest.ToLower())
                {
                    case "q": // quit
                        Tell("Quitting");
                        quit = true;
                        break;

                    case "c": // clear
                        _console.Clear();
                        break;

                    case "h": // help
                        Usage(false);
                        break;

                    default: // user macro?
                        if (_config.Macros.TryGetValue(rest, out var smacro))
                        {
                            Tell(smacro, match: false);
                            //_logger.Trace($">>> [{smacro}]");
                            var td = Encoding.UTF8.GetBytes(smacro);
                            _comm.Send([.. td]);
                        }
                        else
                        {
                            _logger.Error($"Unknown macro name: [{rest}]");
                        }
                        break;
                }
            }
            else // just send verbatim
            {
                var td = Encoding.UTF8.GetBytes(sin);
                _comm.Send([.. td]);
            }

            return quit;
        }

        /// <summary>
        /// Process the receive contents.
        /// </summary>
        /// <param name="r"></param>
        void ProcessReceive(byte[] b)
        {
            // Show whatever arrived.
            var srcv = Encoding.UTF8.GetString(b);
            Tell($"{srcv}", match: true);
            _logger.Trace($"<<< [{srcv}]");
        }
        #endregion

        #region Internal Functions
        /// <summary>
        /// Tell the user something.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clr"></param>
        void Tell(string text, ConsoleColor? clr = null, bool match = false, bool nl = true)
        {
            if (match)
            {
                //  Look for text matches. Internet says simple search is generally faster than compiled regex.
                _config.Matchers.Where(m => text.Contains(m.Key)).ForEach(m => clr = m.Value);
            }

            if (clr is not null) { _console.ForegroundColor = (ConsoleColor)clr; }
            if (nl) _console.WriteLine(text); else _console.Write(text);
            _console.ResetColor();
        }

        /// <summary>
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogMessage(object? sender, LogMessageEventArgs e)
        {
            ConsoleColor? clr = e.Level switch
            {
                LogLevel.Error => _config.ErrorColor,
                LogLevel.Debug => _config.DebugColor,
                _ => null
            };

            Tell(e.ShortMessage, clr: clr, match: true);
        }

        /// <summary>
        /// Tell me everything.
        /// </summary>
        /// <param name="fail">Flavor of infodump</param>
        void Usage(bool fail)
        {
            List<string> docs = [];

            if (fail) docs.Add("NTerm invalid args");

            docs.Add("Execute using one of:");
            docs.Add("    NTerm config_file - See https://github.com/cepthomas/NTerm/blob/main/README.md)");

            void DoOne(List<string> lines)
            {
                int lnum = 0;
                lines.ForEach(l => { docs.Add(lnum++ == 0 ? $"    NTerm {l}" : $"        {l}"); });
            }

            DoOne(TcpComm.Usage());
            DoOne(UdpComm.Usage());
            DoOne(SerialComm.Usage());
            DoOne(NullComm.Usage());

            if (!fail)
            {
                //var ind = $"[{_config.MetaInd}]";
                var ind = Common.MakeReadable((byte)_config.MetaInd);
                docs.Add($"");
                docs.Add($"Commands:");
                docs.Add($"    {ind}q: quit");
                docs.Add($"    {ind}c: clear");
                docs.Add($"    {ind}h: help");
                docs.Add($"    {ind}<macro>: execute macro defined in config file");

                var sp = SerialPort.GetPortNames().ToList();
                if (sp.Count > 0)
                {
                    docs.Add($"");
                    docs.Add($"Serial ports: {string.Join(" ", sp)}");
                }
            }

            var s = string.Join(Environment.NewLine, docs);
            Tell(s, match: false);
        }
        #endregion

        #region Dev Stuff
        /// <summary>
        /// Screwing around.
        /// </summary>
        void Dev()
        {
            Usage(false);

            var s = "[{i}m {i}[0m Hello 🔥";
            Tell(s, ConsoleColor.Green);
            var bs = Encoding.UTF8.GetBytes(s);

            var sx = Common.MakeReadable(bs);
            Tell(sx, ConsoleColor.Green);

            var senc = Encoding.UTF8.GetString(bs);
            Tell(senc, ConsoleColor.Green);


            // void _print(string text) { Print(text, clr: _config.DebugColor); };
            // void _error(string text) { Print(text, clr: _config.ErrorColor); };
            // MiscUtils.GetSourcePath();
            // var scriptFile = Path.Combine(MiscUtils.GetSourcePath(), "Test", "test_script.py");
            // Tools.RunScript(scriptFile, _print, _error);


            //// ConsoleColors
            //var cvals = Enum.GetValues(typeof(ConsoleColor));
            //
            //Console.BackgroundColor = ConsoleColor.Black;
            //Console.WriteLine($"--------------------------------------------------------");
            //for (int i = 0; i < cvals.Length; i++)
            //{
            //    var conclr = (ConsoleColor)i;
            //    Console.ForegroundColor = conclr;
            //    Console.WriteLine($"ForegroundColor:{conclr}");
            //}
            //
            //Console.ForegroundColor = ConsoleColor.White;
            //Console.WriteLine($"--------------------------------------------------------");
            //for (int i = 0; i < cvals.Length; i++)
            //{
            //    var conclr = (ConsoleColor)i;
            //    Console.BackgroundColor = conclr;
            //    Console.WriteLine($"BackgroundColor:{conclr}");
            //}
            //Console.ResetColor();
        }
        #endregion
    }
}
