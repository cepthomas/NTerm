using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>User hot keys.</summary>
        readonly Dictionary<char, string> _hotKeys = [];

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<CliInput> _qCli = new();

        /// <summary>For timing measurements.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Colorizing text.</summary>
        readonly Dictionary<string, ConsoleColorEx> _matchers = [];

        /// <summary>Comm task reporting input.</summary>
        readonly Progress<string> _progress = new();
        // https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap?redirectedfrom=MSDN

        /// <summary>Colorizing text.</summary>
        const ConsoleColor ERR_COLOR = ConsoleColor.Red;
        #endregion

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
            LogManager.LogMessage += (sender, e) => { PrintLine($"{e.Message}"); DoPrompt(); };

            // Set things up.
            try
            {
                _progress.ProgressChanged += (_, s) => { Print(s); };

                // Init stuff.
                ProcessCommandLine();

                Run(); // forever
            }
            catch (IniSyntaxException ex)
            {
                PrintLine($"IniSyntaxException at {ex.LineNum}: {ex.Message}", ERR_COLOR);
                Environment.Exit(1);
            }
            catch (ArgumentException ex)
            {
                PrintLine($"ArgumentException: {ex.Message}", ERR_COLOR);
                Environment.Exit(2);
            }
            catch (Exception ex)
            {
                PrintLine($"{ex.GetType()}: {ex.Message}", ERR_COLOR);
                Environment.Exit(3);
            }
            finally
            {
                // All done.
                _settings.Save();
                LogManager.Stop();
            }
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            // PrintLine("====== Dispose !!!! ======");
            _comm?.Dispose();
        }

        /// <summary>
        /// Main loop.
        /// </summary>
        public void Run()
        {
            DoPrompt();

            using CancellationTokenSource ts = new();
            using Task taskKeyboard = Task.Run(() => DoKeyboard(ts.Token));
            using Task taskKComm = Task.Run(() => _comm.Run(ts.Token));

            //bool timedOut = false;
            bool continuous = false;

            while (!ts.Token.IsCancellationRequested)
            {
                //timedOut = false;

                ///// CLI input? /////
                if (_qCli.TryDequeue(out CliInput? le))
                {
                    // Check for meta key.
                    if (le.Text.Length > 1 && le.Text.StartsWith(_settings.MetaMarker))
                    {
                        switch (le.Text[1])
                        {
                            case 'q':
                                ts.Cancel();
                                break;

                            case 's':
                                var changes = SettingsEditor.Edit(_settings, "NTerm", 500);
                                if (changes.Count > 0)
                                {
                                    PrintLine("Settings changed - please restart");
                                    Environment.Exit(0);
                                }
                                break;

                            case '?':
                                Tools.ShowReadme("NTerm");
                                break;

                            default:
                                _logger.Warn($"Unknown meta key: {le.Text[1]}");
                                break;
                        }
                    }
                    else
                    {
                        _comm.Send(le.Text);

                        //switch (stat)
                        //{
                        //    case OpStatus.Success:
                        //        break;

                        //    case OpStatus.Error:
                        //        _logger.Error($"Comm.Send() error");
                        //        break;

                        //    case OpStatus.ConnectTimeout:
                        //        timedOut = true;
                        //        PrintLine("Server not connecting");
                        //        break;

                        //    case OpStatus.ResponseTimeout:
                        //        timedOut = true;
                        //        PrintLine("Server not responding");
                        //        break;
                        //}
                    }
                    
                    if (!continuous)
                    {
                        DoPrompt();
                    }
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
        /// Task to service the cli read.
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
                    _qCli.Enqueue(new(s, ConsoleModifiers.None));
                }

                // Don't be greedy.
                Thread.Sleep(20);

                //// Check for something to do. TODO1 peek doesn't work. Hot keys.
                //if (Console.KeyAvailable)
                //{
                //    //int ikey = Console.In.Peek();
                //    var conkey = Console.ReadKey(false);
                //    char key = conkey.KeyChar;

                //    // Check for hot key.
                //    if (MatchMods(_settings.HotKeyMod, conkey.Modifiers))
                //    {
                //        _qCli.Enqueue(new(_hotKeys[(char)conkey.Key], conkey.Modifiers));
                //    }
                //    else // Ordinary, get the rest of the line - blocks.
                //    {
                //        var rest = Console.ReadLine();
                //        _qCli.Enqueue(new(key + rest, conkey.Modifiers));
                //    }
                //}

                //// Don't be greedy.
                //Thread.Sleep(20);


            }
        }

        /// <summary>
        /// Process user options.
        /// </summary>
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

                        // [hot_keys]
                        foreach (var val in inrdr.Contents["hot_keys"].Values)
                        {
                            _hotKeys[val.Key[0]] = val.Value; 
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
                    "tcp" => new TcpComm(),
                    //case "udp":
                    //    _comm = new UdpComm();
                    //    break;
                    //case "serial":
                    //    _comm = new SerialComm();
                    //    break;
                    _ => throw new ArgumentException($"Invalid comm type: {ctype}"),
                };
                _comm.Init(args, _progress);
            }
        }

        /// <summary>
        /// Write to console.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="color">Explicit color to use.</param>
        void Print(string text, ConsoleColor? color = null)
        {
            if (color is null)
            {
                foreach (var m in _matchers)
                {
                    if (text.Contains(m.Key)) // faster than compiled regexes
                    {
                        color = (ConsoleColor)m.Value;
                        break;
                    }
                }
            }

            if (color is not null)
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

        /// <summary>
        /// Write to console with NL.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="color">Explicit color to use.</param>
        void PrintLine(string text, ConsoleColor? color = null)
        {
            Print(text + Environment.NewLine, color);
        }

        /// <summary>
        /// Helper.
        /// </summary>
        /// <param name="keyMod"></param>
        /// <param name="consMods"></param>
        /// <returns></returns>
        bool MatchMods(KeyMod keyMod, ConsoleModifiers consMods)
        {
            bool match = false;

            switch (keyMod, consMods)
            {
                case (KeyMod.Ctrl, ConsoleModifiers.Control):
                case (KeyMod.Alt, ConsoleModifiers.Alt):
                case (KeyMod.Shift, ConsoleModifiers.Shift):
                case (KeyMod.CtrlShift, ConsoleModifiers.Control | ConsoleModifiers.Shift):
                    match = true;
                    break;
            }

            return match;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        //void Help() // TODO
        //{
        //    Tools.ShowReadme("NTerm");

        //    _hotKeys.ForEach(x => PrintLine($"{_settings.HotKeyMod}-{x.Key} sends: [{x.Value}]"));

        //    PrintLine("serial ports:");
        //    var sports = SerialPort.GetPortNames();
        //    sports.ForEach(s => { PrintLine($"   {s}"); });
        //}

        /// <summary>
        /// 
        /// </summary>
        void DoPrompt()
        {
            Console.Write(_settings.Prompt);
        }
    }
}
