using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
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

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<string> _qUserCli = new();

        /// <summary>For timing measurements.</summary>
        readonly TimeIt _tmit = new();
        #endregion

        #region Config
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

        /// <summary>User meta keys.</summary>
        readonly Dictionary<char, string> _metaKeys = [];

        /// <summary>Colorizing text.</summary>
        readonly Dictionary<string, ConsoleColorEx> _matchers = [];
        #endregion

        #region Logging
        enum Cat { Send, Receive, Error, Internal }
        FileStream? _logStream = null;
        long _startTick = 0;
        #endregion

        /// <summary>
        /// Build me one.
        /// </summary>
        public App()
        {
            try
            {
                // Init stuff.
                _startTick = Stopwatch.GetTimestamp();
                _logStream = File.Open(Path.Combine(MiscUtils.GetSourcePath(), "nterm.log"), FileMode.Create); // or FileMode.Append

                ProcessAppCommandLine();

                // Go forever.
                Run();
            }
            catch (IniSyntaxException ex)
            {
                Print(Cat.Error, $"IniSyntaxException at {ex.LineNum}: {ex.Message}");
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Print(Cat.Error, $"{ex.GetType()}: {ex}");
                Environment.Exit(2);
            }

            Environment.Exit(0);
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            _comm?.Dispose();
            _logStream?.Dispose();
        }

        /// <summary>
        /// Main loop.
        /// </summary>
        public void Run()
        {
            using CancellationTokenSource ts = new();
            using Task taskKeyboard = Task.Run(() => DoKeyboard(ts.Token));
            using Task taskComm = Task.Run(() => _comm.Run(ts.Token));

            List<char> rcvBuffer = [];

            DoPrompt();

            while (!ts.Token.IsCancellationRequested)
            {
                //timedOut = false;

                ///// User input? /////
                while (_qUserCli.TryDequeue(out string? s))
                {
                    if (s.Length == 0) return;

                    // Check for meta key.
                    if (s[0] == _meta)
                    {
                        if (s.Length > 1)
                        {
                            var hk = s[1];
                            switch (hk)
                            {
                                case 'q': // quit
                                    ts.Cancel();
                                    Task.WaitAll([taskKeyboard, taskComm]);
                                    break;

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
                                        Print(Cat.Error, $"Unknown meta key:{hk}");
                                    }
                                    break;
                            }
                        }
                        // else invalid/ignore
                    }
                    else
                    {
                        Log(Cat.Send, s);
                        _comm.Send(s);
                    }

                    DoPrompt();
                }

                ///// Comm receive? /////
                while (true)
                {
                    var b = _comm.Receive();
                    if (b is not null)
                    {
                        // Look for delimiter or just buffer it.
                        for (int i = 0; i < b.Count(); i++)
                        {
                            if (b[i] == _delim)
                            {
                                // End line.
                                Print(Cat.Receive, string.Concat(rcvBuffer));
                                rcvBuffer.Clear();
                            }
                            else
                            {
                                // Add to buffer.
                                var c = b[i];
                                if (c.IsReadable())
                                {
                                    rcvBuffer.Add((char)c);
                                }
                                else
                                {
                                    var s = $"<{c:0X}>";
                                    rcvBuffer.AddRange(s);
                                }
                            }
                        }
                    }
                    else
                    {
                        break;
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
                inrdr.Contents["meta_keys"].Values.ForEach(val => _metaKeys[val.Key[0]] = val.Value);

                // [matchers] section
                inrdr.Contents["matchers"].Values.ForEach(val => _matchers[val.Key] = Enum.Parse<ConsoleColorEx>(val.Value, true));
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

        /// <summary>
        /// Write a line to console.
        /// </summary>
        /// <param name="cat">Category</param>
        /// <param name="text">What to print</param>
        void Print(Cat cat, string text)
        {
            var color = cat switch
            {
                Cat.Error => _errorColor,
                Cat.Internal => _internalColor,
                _ => ConsoleColorEx.None,
            };

            //  If color not explicitly specified, look through possible matches.
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

            Console.Write(Environment.NewLine);
            Log(cat, text);
        }

        void Log(Cat cat, string text)
        {
            if (_logStream is not null)
            {
                long tick = Stopwatch.GetTimestamp();
                double sec = 1.0 * (tick - _startTick) / Stopwatch.Frequency;
                double msec = 1000.0 * (tick - _startTick) / Stopwatch.Frequency;

                var scat = cat switch
                {
                    Cat.Send => ">>>",
                    Cat.Receive => "<<<",
                    Cat.Error => "!!!",
                    Cat.Internal => "---",
                    _ => throw new NotImplementedException(),
                };

                var s = $"{sec:000.000} {scat} {text}{Environment.NewLine}";
                _logStream.Write(Encoding.Default.GetBytes(s));
                _logStream.Flush();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void DoPrompt()
        {
            Console.Write(_prompt);
        }

        /// <summary>
        /// 
        /// </summary>
        void About()
        {
            Tools.ShowReadme("NTerm");

            // TODO other sys info? Config, meta keys, serial ports,...
            // var cc = _config is not null ? $"{_config.Name}({_config.CommType})" : "None";
            // _hotKeys.ForEach(x => Print($"{_settings.HotKeyMod}-{x.Key} sends: [{x.Value}]"));
            // var sports = SerialPort.GetPortNames();
            // sports.ForEach(s => { Print($"   {s}"); });
        }
    }
}
