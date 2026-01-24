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
using Ephemera.NBagOfTricks;


// A UDP client sends data packets (datagrams) to a SERVER without first establishing a formal connection, 
//   and the UDP server listens for these incoming datagrams.
// TCP - the CLIENT initiates a connection to a listening server


namespace NTerm
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>Current config.</summary>
        Config _config = new();

        /// <summary>Client comm flavor.</summary>
        IComm _comm = new NullComm();

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<string> _qUserCli = new();

        /// <summary>Local logger.</summary>
        readonly FileStream? _logStream = null;

        /// <summary>Logger timestamps.</summary>
        long _startTick = 0;
        #endregion

        /// <summary>
        /// Start here.
        /// </summary>
        static void Main()
        {
            using var app = new App();
        }

        /// <summary>
        /// Build me one and make it go.
        /// </summary>
        public App()
        {
            try
            {
                // Init stuff.
                _startTick = Stopwatch.GetTimestamp();
                var fmode = FileMode.Create; // or .Append TODO1 ??
                _logStream = File.Open(Path.Combine(MiscUtils.GetSourcePath(), "nterm.log"), fmode);

                ProcessAppCommandLine();

                Tell(Cat.Info, $"NTerm using {_comm} - started {DateTime.Now}");

                // Go forever.
                Run();
            }
            // Any exception that arrives here is considered fatal. Inform and exit.
            catch (ConfigException ex) // ini content error
            {
                Tell(Cat.Error, $"{ex.Message}");
                Environment.Exit(1);
            }
            catch (IniSyntaxException ex) // ini structure error
            {
                Tell(Cat.Error, $"Ini syntax error at line {ex.LineNum}: {ex.Message}");
                Environment.Exit(2);
            }
            catch (Exception ex) // other error
            {
                Tell(Cat.Error, ex.ToString());
                Environment.Exit(3);
            }

            Environment.Exit(0);
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            _comm?.Dispose();
            _logStream?.Flush();
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
                    ///// User CLI input? /////
                    while (_qUserCli.TryDequeue(out string? s))
                    {
                        if (s.Length == 0)
                        {
                            Prompt();
                        }
                        else if (s[0] == '\u001b') // Keys.Escape) // _config.MetaInd) // Check for meta key.
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

                                    case "h": // help
                                        About();
                                        Prompt();
                                        break;

                                    default: // user macro?
                                        if (_config.Macros.TryGetValue(mk, out var sk))
                                        {
                                            var td = Encoding.Default.GetBytes(sk).Append(_config.Delim);
                                            _comm.Send([.. td]);
                                        }
                                        else
                                        {
                                            Tell(Cat.Error, $"Unknown macro key: [{mk}]");
                                            Prompt();
                                        }
                                        break;
                                }
                            }
                            // else invalid/ignore
                        }
                        else // just send
                        {
                            Tell(Cat.Send, $"[{s}]");

                            var td = Encoding.Default.GetBytes(s).Append(_config.Delim);
                            _comm.Send([.. td]);
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
                                if (b[i] == _config.Delim)
                                {
                                    // End line.
                                    Tell(Cat.Receive, $"[{string.Concat(rcvBuffer)}]");
                                    rcvBuffer.Clear();
                                    Prompt();
                                }
                                else
                                {
                                    // Add to buffer.
                                    rcvBuffer.Add((char)b[i]);

                                    // TODO option to format non-readable?
                                    //if (b[i].IsReadable())
                                    //{
                                    //    rcvBuffer.Add((char)b[i]);
                                    //}
                                    //else
                                    //{
                                    //    var s = $"<{b[i]:0X}>";
                                    //    rcvBuffer.AddRange(s);
                                    //}
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
                var cmd = "";

                // Check for something to do.
                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey();
                    cmd += k.KeyChar;

                    if (k.Key == ConsoleKey.Escape)
                    {
                        // Meta command. Get the next char.
                        cmd += Console.ReadKey().KeyChar;
                        _qUserCli.Enqueue(new(cmd));
                    }
                    else
                    {
                        // Terminal command.
                        var s = Console.ReadLine();
                        if (s is not null) // && s.Length > 0)
                        {
                            _qUserCli.Enqueue(cmd + s);
                        }
                    }
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        /// <summary>s
        /// Process user command line input.
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

            _config = new();
            _config.Load(args);

            // Process comm spec.
            _comm = _config.CommType[0].ToLower() switch
            {
                "null" => new NullComm(),
                "tcp" => new TcpComm(_config.CommType),
                "udp" => new UdpComm(_config.CommType),
                "serial" => new SerialComm(_config.CommType),
                _ => throw new ConfigException($"Invalid comm type: [{_config.CommType[0]}]"),
            };

            _comm.Notif += (object? _, NotifEventArgs e) => { Tell(e.Cat, e.Message); };
        }

        /// <summary>
        /// Write a line to console and/or log.
        /// </summary>
        /// <param name="cat">Category</param>
        /// <param name="text">What to print</param>
        void Tell(Cat cat, string text)
        {
            bool logit = cat != Cat.Info;
            bool showit = cat != Cat.Log;

            if (showit)
            {
                switch (cat)
                {
                    case Cat.Info:
                        Console.ForegroundColor = _config.InfoColor;
                        break;
                    case Cat.Error:
                        Console.ForegroundColor = _config.ErrorColor;
                        break;
                    default:
                        //  If color not explicitly specified, look for text matches.
                        var mc = _config.Matchers.Where(m => text.Contains(m.Key)); // simple search is faster than compiled regexes
                        if (mc.Any()) Console.ForegroundColor = mc.First().Value;
                        break;
                }

                Console.Write(text);
                Console.Write(Environment.NewLine);
                Console.ResetColor();
            }

            if (logit && _logStream is not null)
            {
                double sec = 1.0 * (Stopwatch.GetTimestamp() - _startTick) / Stopwatch.Frequency;

                var scat = cat switch
                {
                    Cat.Send => ">>>",
                    Cat.Receive => "<<<",
                    Cat.Error => "!!!",
                    _ => "---",
                };

                var s = $"{sec:000.000} {scat} {text}{Environment.NewLine}";
                _logStream.Write(Encoding.Default.GetBytes(s));
                _logStream.Flush();
            }
        }

        /// <summary>
        /// User prompt.
        /// </summary>
        void Prompt()
        {
            Console.Write(_config.Prompt);
        }

        /// <summary>
        /// Show me everything.
        /// </summary>
        void About()
        {
            List<string> docs = ["Execute using one of:"];
            docs.Add("    NTerm tcp host port");
            docs.Add("    NTerm udp host port");
            docs.Add("    NTerm serial port baud framing (e.g. COM1 9600 8N1)");
            docs.Add("    NTerm config_file (TODO1 see https://github.com/cepthomas/NTerm/blob/main/README.md)");

            docs.Add("Commands:");
            docs.Add("    ESC q: quit");
            docs.Add("    ESC h: help");
            docs.Add("    ESC <user macro>");

            docs.Add("Current config:");
            _config.Doc().ForEach(d => docs.Add($"    {d}"));

            var sp = SerialPort.GetPortNames().ToList();
            if (sp.Count > 0)
            {
                docs.Add(Environment.NewLine);
                docs.Add($"Serial ports:");
                sp.ForEach(s => { docs.Add($"- {s}"); });
            }

            docs.ForEach(d => Tell(Cat.Info, d));
            //Tools.MarkdownToHtml(docs, Tools.MarkdownMode.Simple, true); // Simple DarkApi LightApi
        }
    }
}
