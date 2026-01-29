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
using System.Net.Sockets;
using System.Drawing;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>Current config.</summary>
        readonly Config _config = new();

        /// <summary>Module logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("App");

        /// <summary>Client comm flavor.</summary>
        readonly IComm _comm = new NullComm();

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<string> _qUserCli = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Build me one and make it go.
        /// </summary>
        public App()
        {
            int exitCode = 0;

            try
            {
                // Must do this first before initializing.
                string appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");

                // Init logging. TODO log levels from?
                string logFileName = Path.Combine(appDir, "log.txt");
                LogManager.MinLevelFile = LogLevel.Trace;
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
                    var sc = NTerm.Properties.Resources.default_config;
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

                // Say hello.
                _logger.Info($"NTerm using {_comm} {DateTime.Now}");

                // Go forever.
                Run();
            }
            // Any exception that arrives here is considered fatal. Inform and exit.
            catch (ConfigException ex) // known ini error
            {
                _logger.Error($"{ex.Message}");
                exitCode = 1;
            }
            catch (IniSyntaxException ex) // known ini error
            {
                _logger.Error($"Ini syntax error at line {ex.LineNum}: {ex.Message}");
                exitCode = 1;
            }
            catch (Exception ex) // other/unexpected error
            {
                _logger.Exception(ex);
                exitCode = 1;
            }

            LogManager.Stop();

            // Print($"Console is {ConsoleOps.GetRect()}"); // TODO set/save console size??

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
        #endregion~

        #region Main Loop
        /// <summary>
        /// Main loop.
        /// </summary>
        public void Run()
        {
            using CancellationTokenSource ts = new();
            using Task taskKeyboard = Task.Run(() => DoKeyboard(ts.Token));
            using Task taskComm = Task.Run(() => _comm.Run(ts.Token));

            List<char> rcvBuffer = [];

            while (!ts.Token.IsCancellationRequested)
            {
                // Reset current state.
                CommState cst = CommState.Ok;
                Exception? cstexc = null;
               // (CommState cst, Exception exc) state = (CommState.Ok, new());

                try
                {
                    ///// User CLI input /////
                    while (_qUserCli.TryDequeue(out string? sin))
                    {
                        if (sin[0] == ControlChar.ESC) // Check for escape key.
                        {
                            if (sin.Length > 1)
                            {
                                var kin = sin[1];

                                switch (kin)
                                {
                                    case 'q': // quit
                                        ts.Cancel();
                                        Task.WaitAll([taskKeyboard, taskComm]);
                                        break;

                                    case 'c': // clear
                                        Console.Clear();
                                        break;

                                    case 'h': // help
                                        // NewLine();
                                        About(false);
                                        break;

                                    default: // user macro?
                                        if (_config.Macros.TryGetValue(kin, out var smacro))
                                        {
                                            Print(smacro, match: false);
                                            _logger.Trace($">>> [{smacro}]");
                                            var td = Encoding.Default.GetBytes(smacro).Append(_config.Delim);
                                            _comm.Send([.. td]);
                                        }
                                        else
                                        {
                                            _logger.Error($"Unknown macro key: [{FormatByte((byte)kin)}]");
                                        }
                                        break;
                                }
                            }
                            // else invalid/ignore
                        }
                        else // just send
                        {
                            // Print(sin, clr: _config.TrafficColor, match: false);
                            _logger.Trace($">>> [{sin}]");
                            var td = Encoding.Default.GetBytes(sin).Append(_config.Delim);
                            _comm.Send([.. td]);
                        }
                    }

                    ///// Comm receive /////
                    bool rcving = true;
                    while (rcving)
                    {
                        var r = _comm.GetReceive();

                        // Could be message or error or nada.
                        switch (r)
                        {
                            case byte[] b:
                                // Look for delimiter or just buffer it.
                                for (int i = 0; i < b.Length; i++)
                                {
                                    if (b[i] == _config.Delim)
                                    {
                                        // Complete line so process it.
                                        var srcv = string.Concat(rcvBuffer);
                                        Print($"{srcv}", clr: _config.TrafficColor, match: true);
                                        _logger.Trace($"<<< [{srcv}]");
                                        rcvBuffer.Clear();
                                    }
                                    else if (b[i] == ControlChar.CR)
                                    {
                                        // Skip these. Crappy way to handle CRLF. TODO do it correctly.
                                    }
                                    else
                                    {
                                        // Add to buffer.
                                        rcvBuffer.Add((char)b[i]);
                                    }
                                }
                                break;

                            case Exception e:
                                (cst, cstexc) = ProcessException(e);
                                break;

                            default:
                                rcving = false;
                                break;
                        }
                    }

                    ///// Update state /////
                    // Each iteration of the loop may alter the state.
                    switch (cst)
                    {
                        case CommState.Fatal:
                            throw cstexc!;

                        case CommState.Timeout:
                        case CommState.Recoverable:
                            // Nothing. TODO Could add some retry logic?
                            break;

                        case CommState.Ok:
                            // Nothing.
                            break;
                    }

                    // Pace a bit.
                    Thread.Sleep(10);
                }
                catch
                {
                    // Something unexpected. Shut down loop and pass along.
                    ts.Cancel();
                    Task.WaitAll([taskKeyboard, taskComm]);
                    throw;
                }

                Task.WaitAll([taskKeyboard, taskComm]);
            }
        }
        #endregion

        #region Internal Functions
        /// <summary>
        /// General user writer.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clr"></param>
        void Print(string text, ConsoleColor? clr = null, bool match = false, bool nl = true)
        {
            if (match)
            {
                //  Look for text matches. Internet says simple search is generally faster than compiled regex.
                _config.Matchers.Where(m => text.Contains(m.Key)).ForEach(m => clr = m.Value);
            }

            if (clr is not null) { Console.ForegroundColor = (ConsoleColor)clr; }
            if (nl)Console.WriteLine(text); else Console.Write(text);
            Console.ResetColor();
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
                        if (s is not null && s.Length != 0)
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
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            ConsoleColor? clr = e.Level switch
            {
                // LogLevel.Info => _config.InfoColor,
                LogLevel.Error => _config.ErrorColor,
                LogLevel.Debug => _config.DebugColor,
                _ => null
            };

            Print(e.ShortMessage, clr: clr, match: true);
        }

        /// <summary>
        /// Process what happened on the comm thread.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>New state and optional exception</returns>
        (CommState cst, Exception e) ProcessException(Exception e)
        {
            // All the possible exceptions per MS docs:
            // Exception                  ,Description                                                     ,Comm,Function         ,State      
            // ArgumentException          ,                                                                ,SER ,common           ,Fatal      
            // ArgumentNullException      ,endPoint is null.                                               ,UDP ,Connect          ,Fatal      
            // ArgumentNullException      ,The host parameter is null.                                     ,TCP ,ConnectAsync     ,Fatal      
            // ArgumentNullException      ,                                                                ,SER ,common           ,Fatal      
            // ArgumentOutOfRangeException,The port parameter is not between MinPort and MaxPort.          ,TCP ,ConnectAsync     ,Fatal      
            // ArgumentOutOfRangeException,                                                                ,SER ,common           ,Fatal      
            // UnauthorizedAccessException,Access is denied to the port or Already open.                   ,SER ,open             ,Fatal      
            // InvalidOperationException  ,The specified port is already open.                             ,SER ,open             ,Fatal      
            // InvalidOperationException  ,The NetworkStream does not support writing/reading.             ,TCP ,stream.Write/Read,Fatal      
            // InvalidOperationException  ,The specified port is not open.                                 ,SER ,write/read       ,Fatal      
            // InvalidOperationException  ,The TcpClient is not connected to a remote host.                ,TCP ,GetStream        ,Fatal      
            // IOException                ,The port is in an invalid state or invalid.                     ,SER ,open             ,Fatal      
            // IOException                ,Error when accessing the socket or network read/write failure.  ,TCP ,stream.Write/Read,Recoverable
            // ObjectDisposedException    ,TcpClient is closed.                                            ,TCP ,ConnectAsync     ,Recoverable
            // ObjectDisposedException    ,The NetworkStream is closed.                                    ,TCP ,stream.Write/Read,Recoverable
            // ObjectDisposedException    ,The TcpClient has been closed.                                  ,TCP ,GetStream        ,Recoverable
            // ObjectDisposedException    ,The UdpClient is closed.                                        ,UDP ,Connect          ,Recoverable
            // ObjectDisposedException    ,The underlying Socket has been closed.                          ,UDP ,ReceiveAsync     ,Recoverable
            // OperationCanceledException ,The cancellation token was canceled. Exception in returned task.,TCP ,ConnectAsync     ,Recoverable
            // SocketException            ,Error when accessing the socket.                                ,TCP ,ConnectAsync     ,SPECIAL    
            // SocketException            ,Error when accessing the socket.                                ,UDP ,Connect          ,SPECIAL    
            // SocketException            ,Error when accessing the socket.                                ,UDP ,ReceiveAsync     ,SPECIAL    
            // TimeoutException           ,The operation did not complete before the timeout period ended. ,SER ,write/read       ,Timeout    

            // Async ops carry the original exception in inner.
            if (e is AggregateException)
            {
                e = e.InnerException ?? e;
            }

            CommState cst;
            switch (e)
            {
                case OperationCanceledException:
                case ObjectDisposedException:
                case IOException:
                    cst = CommState.Recoverable;
                    break;

                case TimeoutException:
                    cst = CommState.Timeout;
                    break;

                case SocketException ex:
                    // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                    int[] valid = [10053, 10054, 10060, 10061, 10064];
                    cst = valid.Contains(ex.NativeErrorCode) ? CommState.Recoverable : CommState.Fatal;
                    break;

                case ArgumentNullException:
                case ArgumentOutOfRangeException:
                case ArgumentException:
                case UnauthorizedAccessException:
                case InvalidOperationException:
                default:
                    cst = CommState.Fatal;
                    break;
            }

            return (cst, e);
        }

        /// <summary>
        /// Format non-readable for human consumption.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        string FormatByte(byte b)
        {
            if (b.IsReadable())
            {
                return ((char)b).ToString();
            }
            else
            {
                Keys k = (Keys)b;
                return k.ToString();
            }
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

            var s = string.Join(Environment.NewLine, docs);
            Print(s, match: false);
        }
        #endregion

        #region Dev Stuff
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
        #endregion
    }

    /// <summary>
    /// Manipulate the console window using win32 functions.
    /// </summary>
    public class ConsoleOps
    {
        struct RectNative
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Constants for the ShowWindow function
        const int SW_MAXIMIZE = 3;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RectNative lpRect);

        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        public static void Move(Rectangle rect)
        {
            IntPtr hnd = GetForegroundWindow();
            MoveWindow(hnd, rect.Left, rect.Top, rect.Width, rect.Height, true);
        }

        public static Rectangle GetRect()
        {
            IntPtr hnd = GetForegroundWindow();
            GetWindowRect(hnd, out RectNative nrect);
            return new Rectangle(nrect.Left, nrect.Top, nrect.Right - nrect.Left, nrect.Bottom - nrect.Top);
        }
    }
}
