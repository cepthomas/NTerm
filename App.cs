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
        // readonly Logger _logger = LogManager.CreateLogger("APP");

        ///// <summary>Settings</summary>
        //readonly UserSettings _settings = new();

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

        /// <summary>Color for error messages.</summary>
        ConsoleColorEx _errorColor = ConsoleColorEx.Red; // default

        /// <summary>Color for internal messages.</summary>
        ConsoleColorEx _internalColor = ConsoleColorEx.Blue; // default

        /// <summary>Prompt. Can be empty for continuous receiving.</summary>
        string _prompt = ""; // default

        /// <summary>Indicator for application functions.</summary>
        char _meta = '-'; // default

        /// <summary>Message delimiter: LF|CR|NUL.</summary>
        byte _delim = 10; // default LF




        /// <summary>
        /// Build me one.
        /// </summary>
        public App()
        {
            //TimeIt.Snap("App()");

            //// Get settings.
            //var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            //_settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            //// Set up log first.
            //var logFileName = Path.Combine(appDir, "log.txt");
            //LogManager.MinLevelFile = _settings.FileLogLevel;
            //LogManager.MinLevelNotif = _settings.NotifLogLevel;
            //LogManager.Run(logFileName, 50000);
            //LogManager.LogMessage += (sender, e) => { PrintLine($"{e.Message}", _settings.IntColor); DoPrompt(); };

            // Set things up.
            try
            {
                // Init stuff.
                ProcessAppCommandLine();

                // Go forever.
                Run();
            }
            catch (IniSyntaxException ex)
            {
                PrintLine($"IniSyntaxException at {ex.LineNum}: {ex.Message}", _errorColor);
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                PrintLine($"{ex.GetType()}: {ex}", _errorColor);
                Environment.Exit(2);
            }
            finally
            {
                // All done.
                //_settings.Save();
                //LogManager.Stop();
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
                    if (s.Length > 1 && s.StartsWith(_meta))
                    {
                        var hk = s[1];
                        switch (hk)
                        {
                            case 'q': // quit
                                ts.Cancel();
                                Task.WaitAll([taskKeyboard, taskComm]);
                                break;

                            //case 's': // edit settings
                            //    var changes = SettingsEditor.Edit(_settings, "NTerm", 500);
                            //    if (changes.Count > 0)
                            //    {
                            //        Print("Settings changed - please restart", _settings.IntColor);
                            //    }
                            //    break;

                            case '?': // help
                                About();
                                break;

                            default: // user meta key?
                                if (_metaKeys.TryGetValue(hk, out var sk))
                                {
                                    _comm.Send(sk);
                                }
                                else
                                {
                                    Print($"Unknown meta key:{sk}", _internalColor);
                                }
                                break;
                        }
                    }
                    else
                    {
                        _comm.Send(s);
                    }

                    DoPrompt();

                    //if (!continuous)
                    //{
                    //    DoPrompt();
                    //}
                }

                ///// Comm receive? /////
                while (true)
                {
                    var s = _comm.Receive(); // TODO1 needs delim - ini.
                    if (s == null) break;
                    Print("TODO1");
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

                    if (s is not null && s.Length > 0)
                    {
                        _qUserCli.Enqueue(new(s));
                    }
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        /// <summary>s
        /// Process user command line input. Could be explicit comm spec or a config file name.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="IniSyntaxException"></exception>
        void ProcessAppCommandLine()
        {
            var args = Environment.GetCommandLineArgs().ToList()[1..];

            if (args.Count == 0)
            {
                About();
                return;
            }

            List<string> commSpec = [];

            // Check for ini file first.
            if (args[0].EndsWith(".ini") && File.Exists(args[0]))
            {
                // OK process it.
                var inrdr = new IniReader(args[0]);

                // [nterm] section
                foreach (var nval in inrdr.Contents["nterm"].Values)
                {
                    switch (nval.Key)
                    {
                        case "comm_type":
                            commSpec = nval.Value.SplitByToken(" ");
                            break;

                        case "err_color":
                            _errorColor = Enum.Parse<ConsoleColorEx>(nval.Value, true);
                            break;

                        case "internal_color":
                            _errorColor = Enum.Parse<ConsoleColorEx>(nval.Value, true);
                            break;

                        case "prompt":
                            _prompt = nval.Value;
                            break;

                        case "meta":
                            _meta = nval.Value[0];
                            break;

                        case "delim":
                            _delim = nval.Value switch
                            {
                                "LF" => 10,
                                "CR" => 13,
                                "NUL" => 0,
                                _ => throw new IniSyntaxException($"Invalid delim: {nval.Value}", -1),
                            };
                            break;

                        default:
                            throw new IniSyntaxException($"Invalid [nterm] section key: {nval.Key}", -1);
                    }
                }

                // [meta_keys] section
                foreach (var val in inrdr.Contents["meta_keys"].Values)
                {
                    _metaKeys[val.Key[0]] = val.Value;
                }

                // [matchers] section
                foreach (var val in inrdr.Contents["matchers"].Values)
                {
                    _matchers[val.Key] = Enum.Parse<ConsoleColorEx>(val.Value, true);
                }
            }
            else // assume explicit cl spec
            {
                commSpec = args;
            }

            // Process comm spec.
            if (commSpec.Count > 0)
            {
                _comm = commSpec[0] switch
                {
                    "null" => new NullComm(),
                    "tcp" => new TcpComm(args),
                    "udp" => new UdpComm(args),
                    "serial" => new SerialComm(args),
                    _ => throw new IniSyntaxException($"Invalid comm type: {args[0]}", -1),
                };
            }
        }


/*
            switch (args[0])
            {
                case "null":
                case "tcp":
                case "udp":
                case "serial":
                    ProcessCommType(args);
                    break;

                case "?":
                    About();
                    break;

                default:
                    // Valid ini file?
                    if (args[0].EndsWith(".ini") && File.Exists(args[0]))
                    {
                        var inrdr = new IniReader(args[0]);

                        // [nterm]
                        foreach (var val in inrdr.Contents["nterm"].Values)
                        {
                            switch (val.Key)
                            {
                                case "comm_type":
                                    ProcessCommType(val.Value.SplitByToken(" "));
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
            void ProcessCommType(List<string> args)
            {
                _comm = args[0] switch
                {
                    "null" => new NullComm(),
                    "tcp" => new TcpComm(args),
                    "udp" => new UdpComm(args),
                    "serial" => new SerialComm(args),
                    _ => throw new IniSyntaxException($"Invalid comm type: {args[0]}", -1),
                };
            }
*/

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
                    //Console.BackgroundColor = (ConsoleColor)color;
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
                Console.ForegroundColor = (ConsoleColor)color;
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

        /// <summary>
        /// 
        /// </summary>
        void About()
        {
            Tools.ShowReadme("NTerm"); // TODO other sys info?
        }
    }
}
