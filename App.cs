using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;



namespace NTerm
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>My logger</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>Settings</summary>
        readonly UserSettings _settings = new();

        /// <summary>Client comm flavor.</summary>
        IComm _comm = new NullComm();

        /// <summary>User meta keys.</summary>
        readonly Dictionary<char, string> _metaKeys = [];

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<string> _qUserCli = new();

        /// <summary>For timing measurements.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Colorizing text.</summary>
        readonly Dictionary<string, ConsoleColorEx> _matchers = [];
        #endregion


        bool continuous = false; // TODO1 how to handle this option vs cmd/resp. Also handle prompt. meta to stop/start recv.


        /// <summary>
        /// Build me one.
        /// </summary>
        public App()
        {
            //TimeIt.Snap("App()");

            // Get settings.
            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            // Set up log first.
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += (sender, e) => { PrintLine($"{e.Message}", _settings.IntColor); DoPrompt(); };

            // Set things up.
            try
            {
                // Init stuff.
                ProcessCommandLine();

                // Go forever.
                Run();
            }
            catch (IniSyntaxException ex)
            {
                PrintLine($"IniSyntaxException at {ex.LineNum}: {ex.Message}", _settings.ErrColor);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                PrintLine($"{ex.GetType()}: {ex}", _settings.ErrColor);
                Environment.Exit(2);
            }
            finally
            {
                // All done.
                _settings.Save();
                LogManager.Stop();
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            // PrintLine("====== Dispose !!!! ======", _settings.IntColor);
            _comm?.Dispose();
        }

        /// <summary>
        /// Main loop.
        /// </summary>
        public void Run()
        {
            using CancellationTokenSource ts = new();
            using Task taskKeyboard = Task.Run(() => DoKeyboard(ts.Token));
            using Task taskComm = Task.Run(() => _comm.Run(ts.Token));

            DoPrompt();

            while (!ts.Token.IsCancellationRequested)
            {
                //timedOut = false;

                ///// User input? /////
                while (_qUserCli.TryDequeue(out string? s))
                {
                    // Check for meta key.
                    if (s.Length > 1 && s.StartsWith(_settings.MetaMarker))
                    {
                        var hk = s[1];
                        switch (hk)
                        {
                            case 'q':
                                ts.Cancel();
                                Task.WaitAll([taskKeyboard, taskComm]);
                                break;

                            case 's':
                                var changes = SettingsEditor.Edit(_settings, "NTerm", 500);
                                if (changes.Count > 0)
                                {
                                    PrintLine("Settings changed - please restart", _settings.IntColor);
                                    Environment.Exit(0);
                                }
                                break;

                            case '?':
                                Tools.ShowReadme("NTerm");
                                break;

                            default:
                                // Check for user meta key.
                                if (_metaKeys.TryGetValue(hk, out var sk))
                                {
                                    _comm.Send(sk);
                                }
                                else
                                {
                                    _logger.Warn($"Unknown meta key:{sk}");
                                }
                                break;
                        }
                    }
                    else
                    {
                        _comm.Send(s);
                    }

                    if (!continuous)
                    {
                        DoPrompt();
                    }
                }

                ///// Comm receive? /////
                while (true)
                {
                    var s = _comm.Receive();
                    if (s == null) break;
                    Print(s);
                }

                // Delay a bit.
                Thread.Sleep(10);

                //// If there was no timeout, delay a bit.
                //if (!timedOut)
                //{
                //    Thread.Sleep(10);
                //}
            }
        }

        /// <summary>
        /// Task to service the user input read.
        /// </summary>
        /// <param name="token"></param>
        void DoKeyboard(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Check for something to do.
                if (Console.KeyAvailable)
                {
                    var s = Console.ReadLine();
                    _qUserCli.Enqueue(new(s));
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        /// <summary>
        /// Process user input.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="IniSyntaxException"></exception>
        void ProcessCommandLine()
        {
            var args = Environment.GetCommandLineArgs().ToList()[1..];

            if (args.Count == 0)
            {
                throw new ArgumentException($"Invalid command line");
            }

            switch (args[0])
            {
                case "null":
                case "tcp":
                case "udp":
                case "serial":
                    ProcessCommType(args[0], args[1..]);
                    break;

                case "?":
                    Tools.ShowReadme("NTerm");
                    Environment.Exit(0);
                    break;

                default:
                    // Check for valid file.
                    if (args[0].EndsWith(".ini") && File.Exists(args[0]))
                    {
                        var inrdr = new IniReader(args[0]);

                        // [nterm]
                        foreach (var val in inrdr.Contents["nterm"].Values)
                        {
                            switch (val.Key)
                            {
                                case "comm_type":
                                    ProcessCommType(val.Key, val.Value.SplitByToken(" "));
                                    break;
                                default:
                                    throw new IniSyntaxException($"Invalid section value for {val.Key}", -1);
                            }
                        }

                        // [meta_keys]
                        foreach (var val in inrdr.Contents["meta_keys"].Values)
                        {
                            _metaKeys[val.Key[0]] = val.Value;
                        }

                        // [matchers]
                        foreach (var val in inrdr.Contents["matchers"].Values)
                        {
                            _matchers[val.Key] = Enum.Parse<ConsoleColorEx>(val.Value, true);
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Invalid ini file: {args[0]}");
                    }
                    break;
            }

            ///// Local function. /////
            void ProcessCommType(string ctype, List<string> args)
            {
                _comm = ctype switch
                {
                    "null" => new NullComm(),
                    "tcp" => new TcpComm(args),
                    "udp" => new UdpComm(args),
                    "serial" => new SerialComm(args),
                    _ => throw new ArgumentException($"Invalid comm type: {ctype}"),
                };
            }
        }

        /// <summary>
        /// Write to console.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="color">Explicit color to use.</param>
        void Print(string text, ConsoleColorEx color = ConsoleColorEx.None)
        {
            if (color == ConsoleColorEx.None)
            {
                foreach (var m in _matchers)
                {
                    if (text.Contains(m.Key)) // faster than compiled regexes
                    {
                        color = m.Value;
                        break;
                    }
                }

                if (color != ConsoleColorEx.None)
                {
                    Console.ForegroundColor = (ConsoleColor)color;
                    Console.Write(text);
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(text);
                }
            }
            else
            {
                Console.BackgroundColor = (ConsoleColor)color;
                Console.Write(text);
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Write to console with NL.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="color">Explicit color to use.</param>
        void PrintLine(string text, ConsoleColorEx color = ConsoleColorEx.None)
        {
            Print(text + Environment.NewLine, color);
        }

        /// <summary>
        /// 
        /// </summary>
        void DoPrompt()
        {
            //TODO1? Console.Write(_settings.Prompt);
        }
    }
}
