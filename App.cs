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
        /// <summary>Client comm flavor.</summary>
        IComm _comm = new NullComm();

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<string> _qUserCli = new();

        /// <summary>Local logger.</summary>
        readonly FileStream? _logStream = null;

        /// <summary>Logger timestamps.</summary>
        readonly long _startTick = Stopwatch.GetTimestamp();

        /// <summary>For timing measurements.</summary>
        readonly TimeIt _tmit = new();
        #endregion

        #region Config
        /// <summary>Color for error messages.</summary>
        ConsoleColorEx _errorColor = ConsoleColorEx.Red; // default

        /// <summary>Color for internal messages.</summary>
        ConsoleColorEx _infoColor = ConsoleColorEx.Blue; // default

        /// <summary>Prompt. Can be empty for continuous receiving.</summary>
        string _prompt = ""; // default

        /// <summary>Indicator for application functions.</summary>
        char _meta = '!'; // default

        /// <summary>Message delimiter: LF|CR|NUL.</summary>
        byte _delim = 10; // default LF (or 13 00)

        /// <summary>User macros.</summary>
        readonly Dictionary<string, string> _macros = [];

        /// <summary>Colorizing text.</summary>
        readonly Dictionary<string, ConsoleColorEx> _matchers = [];
        #endregion

        /// <summary>
        /// Build me one.
        /// </summary>
        public App()
        {
            try
            {
                // Init stuff.
                var fmode = FileMode.Create; // or .Append ??
                _logStream = File.Open(Path.Combine(MiscUtils.GetSourcePath(), "nterm.log"), fmode);

                ProcessAppCommandLine();

                Print(Cat.None, $"NTerm using {_comm}");

                // Go forever.
                Run();
            }
            catch (ConfigException ex)
            {
                Print(Cat.Error, $"{ex.Message}");
                Environment.Exit(1);
            }
            catch (IniSyntaxException ex)
            {
                Print(Cat.Error, $"Ini syntax error at {ex.LineNum}: {ex.Message}");
                Log(Cat.Error, ex.ToString());
                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Print(Cat.Error, $"{ex.GetType()}: {ex.Message}");
                Log(Cat.Error, ex.ToString());
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

            Prompt();

            while (!ts.Token.IsCancellationRequested)
            {
                try
                {
                    ///// User input? /////
                    while (_qUserCli.TryDequeue(out string? s))
                    {
                        if (s.Length == 0) return;

                        // Check for meta key.
                        if (s[0] == _meta)
                        {
                            if (s.Length > 1)
                            {
                                var mk = s[1..];

                                switch (mk)
                                {
                                    case "q": // quit
                                        ts.Cancel();
                                        Task.WaitAll([taskKeyboard, taskComm]);
                                        break;

                                    case "?": // help
                                        About();
                                        Prompt();
                                        break;

                                    default: // user macro?
                                        if (_macros.TryGetValue(mk, out var sk))
                                        {
                                            _comm.Send(sk);
                                        }
                                        else
                                        {
                                            Print(Cat.Error, $"Unknown macro key: [{mk}]");
                                            Prompt();
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
                    }

                    ///// Comm receive? /////
                    while (true)
                    {
                        var b = _comm.Receive();
                        if (b is not null)
                        {
                            // Look for delimiter or just buffer it.
                            for (int i = 0; i < b.Length; i++)
                            {
                                if (b[i] == _delim)
                                {
                                    // End line.
                                    Print(Cat.Receive, string.Concat(rcvBuffer));
                                    rcvBuffer.Clear();
                                    Prompt();
                                }
                                else
                                {
                                    // Add to buffer. Make non-readable friendlier.
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
                }
                catch (Exception)
                {
                    // Something went wrong. Must shut down gracefully before passing exception up.
                    ts.Cancel();
                    Task.WaitAll([taskKeyboard, taskComm]);
                    throw;
                }
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
        /// <exception cref="ConfigException"></exception>
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
            if (args[0].EndsWith(".ini"))
            {
                if (!File.Exists(args[0]))
                {
                    throw new ConfigException($"Invalid config file: [{args[0]}]");
                }

                // OK process it.
                var inrdr = new IniReader(args[0]);

                // [nterm] section
                foreach (var nval in inrdr.Contents["nterm"].Values)
                {
                    switch (nval.Key.ToLower())
                    {
                        case "comm_type":
                            commSpec = nval.Value.SplitByToken(" ");
                            break;

                        case "err_color":
                            _errorColor = Enum.Parse<ConsoleColorEx>(nval.Value, true);
                            break;

                        case "info_color":
                            _infoColor = Enum.Parse<ConsoleColorEx>(nval.Value, true);
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
                                _ => throw new ConfigException($"Invalid delim: [{nval.Value}]"),
                            };
                            break;

                        default:
                            throw new ConfigException($"Invalid [nterm] section key: [{nval.Key}]");
                    }
                }

                // [macros] section
                inrdr.Contents["macros"].Values.ForEach(val => _macros[val.Key] = val.Value.Replace("\"", ""));

                // [matchers] section
                inrdr.Contents["matchers"].Values.ForEach(val => _matchers[val.Key.Replace("\"", "")] = Enum.Parse<ConsoleColorEx>(val.Value, true));
            }
            else // assume explicit cl spec
            {
                commSpec = args;
            }

            // Process comm spec.
            if (commSpec.Count > 0)
            {
                _comm = commSpec[0].ToLower() switch
                {
                    "null" => new NullComm(),
                    "tcp" => new TcpComm(commSpec),
                    "udp" => new UdpComm(commSpec),
                    "serial" => new SerialComm(commSpec),
                    _ => throw new ConfigException($"Invalid comm type: [{commSpec[0]}]"),
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
            var catColor = cat switch
            {
                Cat.Error => _errorColor,
                Cat.Info => _infoColor,
                _ => ConsoleColorEx.None
            };

            //  If color not explicitly specified, look for text matches.
            if (catColor == ConsoleColorEx.None)
            {
                foreach (var m in _matchers)
                {
                    if (text.Contains(m.Key)) // faster than compiled regexes
                    {
                        catColor = m.Value;
                        break;
                    }
                }
            }

            if (catColor != ConsoleColorEx.None)
            {
                Console.ForegroundColor = (ConsoleColor)catColor;
            }

            Console.Write(text);
            Console.Write(Environment.NewLine);
            Console.ResetColor();
            
            Log(cat, text);
        }

        /// <summary>
        /// Write to logger.
        /// </summary>
        /// <param name="cat">Category</param>
        /// <param name="text">What to print</param>
        void Log(Cat cat, string text)
        {
            if (_logStream is not null)
            {
                long tick = Stopwatch.GetTimestamp();
                double sec = 1.0 * (tick - _startTick) / Stopwatch.Frequency;
                //double msec = 1000.0 * (tick - _startTick) / Stopwatch.Frequency;

                var scat = cat switch
                {
                    Cat.Send => ">>>",
                    Cat.Receive => "<<<",
                    Cat.Error => "!!!",
                    Cat.Info => "---",
                    Cat.None => "---",
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
        void Prompt()
        {
            Console.Write(_prompt);
        }

        /// <summary>
        /// Show me everything.
        /// </summary>
        void About()
        {
            //var docs = File.ReadLines("xxREADME.md").ToList();
            List<string> docs = ["Main doc at github.com/cepthomas/NTerm/blob/main/README.md"];

            //docs.Add("");
            docs.Add("# Current Configuration");
            docs.Add($"- comm_type:{_comm}");
            docs.Add($"- delim:{_delim}");
            docs.Add($"- prompt:{_prompt}");
            docs.Add($"- meta:{_meta}");
            docs.Add($"- info_color:{_infoColor}");
            docs.Add($"- err_color:{_errorColor}");

            if (_macros.Count > 0)
            {
                docs.Add("");
                docs.Add($"macros:");
                _macros.ForEach(m => docs.Add($"- {m.Key}:{m.Value}"));
            }

            if (_matchers.Count > 0)
            {
                docs.Add("");
                docs.Add($"matchers:");
                _matchers.ForEach(m => docs.Add($"- {m.Key}:{m.Value}"));
            }

            var sp = SerialPort.GetPortNames().ToList();
            if (sp.Count > 0)
            {
                docs.Add("");
                docs.Add("serial ports:");
                sp.ForEach(s => { docs.Add($"- {s}"); });
            }

            docs.ForEach(d => Print(Cat.None, d));
            //Tools.MarkdownToHtml(docs, Tools.MarkdownMode.Simple, true); // Simple DarkApi LightApi
        }
    }
}
