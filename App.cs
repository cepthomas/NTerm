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
using System.Runtime.InteropServices;
using Ephemera.NBagOfTricks;
using NTerm.Properties;


namespace NTerm
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>Current config.</summary>
        readonly Config _config = new();

        /// <summary>Module logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>Client comm flavor.</summary>
        readonly IComm _comm = new NullComm();

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<string> _qUserCli = new();

        /// <summary>Logger timestamps.</summary>
        readonly long _startTick = 0;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Build me one and make it go.
        /// </summary>
        public App()
        {
            //Dev();
            //Environment.Exit(0);

            int exitCode = 0;

            try
            {
                // Init stuff.
                _startTick = Stopwatch.GetTimestamp();

                // Must do this first before initializing.
                string appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");

                // Init logging. TODO log levels from?
                string logFileName = Path.Combine(appDir, "log.txt");
                LogManager.MinLevelFile = LogLevel.Debug;
                LogManager.MinLevelNotif = LogLevel.Info;
                LogManager.LogMessage += LogManager_LogMessage;
                LogManager.Run(logFileName, 100000);

                // Process user command line input.
                var args = Environment.GetCommandLineArgs().ToList()[1..];

                if (args.Count == 0)
                {
                    About(true);
                    Environment.Exit(1);
                }

                // Load config. Is there a default ini? if no, copy from resources.
                string defaultConfig = Path.Combine(appDir, "default.ini");
                if (!File.Exists(defaultConfig))
                {
                    var sc = Resources.default_config;
                    File.WriteAllText(defaultConfig, sc.ToString());
                }

                _config = new();
                _config.Load(args, defaultConfig);

                // Process comm spec.
                _comm = _config.CommConfig[0].ToLower() switch
                {
                    "null" => new NullComm(),
                    "tcp" => new TcpComm(_config.CommConfig),
                    "udp" => new UdpComm(_config.CommConfig),
                    "serial" => new SerialComm(_config.CommConfig),
                    _ => throw new ConfigException($"Invalid comm type: [{_config.CommConfig[0]}]"),
                };

                _logger.Info($"NTerm using {_comm} - started {DateTime.Now}");

                // Go forever.
                Run();
            }
            // Any exception that arrives here is considered fatal. Inform and exit.
            catch (ConfigException ex) // ini content error
            {
                _logger.Error($"{ex.Message}");
                exitCode = 1;
            }
            catch (IniSyntaxException ex) // ini structure error
            {
                _logger.Error($"Ini syntax error at line {ex.LineNum}: {ex.Message}");
                exitCode = 1;
            }
            catch (Exception ex) // other error
            {
                _logger.Error(ex.ToString());
                exitCode = 1;
            }

            LogManager.Stop();

            // Wait to let logging finish.
            //Thread.Sleep(100);

            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            _comm?.Dispose();
        }

        /// <summary>
        /// Start here.
        /// </summary>
        static void Main()
        {
            using var app = new App();
        }
        #endregion

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
                    //=========== Check state ============//
                    // TODO1 - state/retries etc.


                    ///// User CLI input? /////
                    while (_qUserCli.TryDequeue(out string? sin))
                    {
                        if (sin.Length == 0)
                        {
                            Prompt();
                        }
                        else if (sin[0] == Defs.ESC) // Check for escape key.
                        {
                            if (sin.Length > 1)
                            {
                                var kin = sin[1];
                                //var mk = s[1..];

                                switch (kin)
                                {
                                    case 'q': // quit
                                        ts.Cancel();
                                        Task.WaitAll([taskKeyboard, taskComm]);
                                        break;

                                    case 'c': // clear TODO1
                                        Console.Clear();
                                        Prompt();
                                        break;

                                    case 'h': // help
                                        About(false);
                                        Prompt();
                                        break;

                                    default: // user macro?
                                        if (_config.Macros.TryGetValue(kin, out var smacro))
                                        {
                                            var td = Encoding.Default.GetBytes(smacro).Append(_config.Delim);
                                            _comm.Send([.. td]);
                                        }
                                        else
                                        {
                                            _logger.Error($"Unknown macro key: [{Utils.FormatByte((byte)kin)}]");
                                            Prompt();
                                        }
                                        break;
                                }
                            }
                            // else invalid/ignore
                        }
                        else // just send
                        {
                            _logger.Debug($">>> [{sin}]");
                            var td = Encoding.Default.GetBytes(sin).Append(_config.Delim);
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
                                    //_logger.Info($"{string.Concat(rcvBuffer)}]");
                                    _logger.Info($"<<< [{string.Concat(rcvBuffer)}]");
                                    //_logger.Debug($"<<< [{string.Concat(rcvBuffer)}]");
                                    rcvBuffer.Clear();
                                    Prompt();
                                }
                                else
                                {
                                    // Add to buffer.
                                    rcvBuffer.Add((char)b[i]);
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
                catch (Exception e)
                {
                    //var state = Utils.ProcessException(e);



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
                var kbdin = "";

                // Check for something to do.
                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey();
                    kbdin += k.KeyChar;

                    if (k.Key == ConsoleKey.Escape)
                    {
                        // Meta command. Get the next char.
                        kbdin += Console.ReadKey().KeyChar;
                        _qUserCli.Enqueue(new(kbdin));
                    }
                    else
                    {
                        // Terminal command.
                        var s = Console.ReadLine();
                        if (s is not null)
                        {
                            _qUserCli.Enqueue(kbdin + s);
                        }
                    }
                }

                // Don't be greedy.
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// General user writer.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clr"></param>
        void Print(string text, ConsoleColor? clr = null)
        {
            Console.ForegroundColor = clr ?? _config.InfoColor;
            Console.WriteLine(text);
            Console.ResetColor();
            Prompt();
        }

        /// <summary>
        /// User prompt.
        /// </summary>
        void Prompt()
        {
            Console.ForegroundColor = _config.InfoColor;
            Console.Write(_config.Prompt);
            Console.ResetColor();
        }

        /// <summary>
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            ConsoleColor? clr = null;

            //  Look for text matches. Note simple search is generally faster than regex.
            var mc = _config.Matchers.Where(m => e.Message.Contains(m.Key));
            if (mc.Any()) clr = mc.First().Value;

            clr ??= e.Level switch
            {
                LogLevel.Info => _config.InfoColor,
                LogLevel.Error => _config.ErrorColor,
                LogLevel.Debug => _config.DebugColor,
                _ => null
            };

            Print(e.ShortMessage, clr);
        }

        /// <summary>
        /// Show me everything.
        /// </summary>
        /// <param name="fail">Flavor of infodump</param>
        void About(bool fail)
        {
            List<string> docs = [];

            if (!fail)
            {
                docs.Add("NTerm usage");
            }
            else
            {
                docs.Add("NTerm invalid args");
            }

            docs.Add("Execute using one of:");
            docs.Add("    NTerm <config_file> - See https://github.com/cepthomas/NTerm/blob/main/README.md)");
            docs.Add("    NTerm tcp <host> <port>");
            docs.Add("    NTerm udp <host> <port>");
            docs.Add("    NTerm serial <port> <baud> <framing> - e.g. COM1 9600 8N1");

            if (!fail)
            {
                docs.Add("Commands:");
                docs.Add("    ESC q: quit");
                docs.Add("    ESC c: clear");
                docs.Add("    ESC h: help");
                docs.Add("    ESC <macro>: execute macro defined in config file");

                docs.Add("Current config:");
                _config.Doc().ForEach(d => docs.Add($"    {d}"));

                var sp = SerialPort.GetPortNames().ToList();
                if (sp.Count > 0)
                {
                    docs.Add(Environment.NewLine);
                    docs.Add($"Serial ports:");
                    sp.ForEach(s => { docs.Add($"- {s}"); });
                }
            }

            Print(string.Join(Environment.NewLine, docs));
        }

        /// <summary>
        /// Screwing around.
        /// </summary>
        void Dev()
        {
            //ConsoleOps.Move(50, 50, 1000, 900);

            var cvals = Enum.GetValues(typeof(ConsoleColor));

            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($"--------------------------------------------------------");
            for (int i = 0; i < cvals.Length; i++)
            {
                var conclr = (ConsoleColor)i;
                Console.ForegroundColor = conclr;
                Console.WriteLine($"ForegroundColor:{conclr}");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"--------------------------------------------------------");
            for (int i = 0; i < cvals.Length; i++)
            {
                var conclr = (ConsoleColor)i;
                Console.BackgroundColor = conclr;
                Console.WriteLine($"BackgroundColor:{conclr}");
            }
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Manipulate the console window using win32 functions. TODO1 set/save console size??
    /// </summary>
    public class ConsoleOps
    {
        // Structure used by GetWindowRect - pixels.
        struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        // Constants for the ShowWindow function
        const int SW_MAXIMIZE = 3;

        public static void Move(int x, int y, int w, int h)
        {
            // Get the handle of the console.
            IntPtr hnd = GetForegroundWindow();

            // Resize and reposition the console window to fill the screen
            MoveWindow(hnd, x, y, w, h, true);

            // Maximize the console.
            //ShowWindow(hnd, SW_MAXIMIZE);

            // Get the screen size.
            //Rect screenRect;
            //GetWindowRect(hnd, out screenRect);
            //int width = screenRect.Right - screenRect.Left;
            //int height = screenRect.Bottom - screenRect.Top;
        }
    }
}
