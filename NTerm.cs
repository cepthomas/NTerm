using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Ephemera.NBagOfTricks; // TODO update this.
using Ephemera.NBagOfTricks.Slog;
//using Ephemera.NBagOfUis;


namespace NTerm
{
    public class App
    {
        #region Fields
        /// <summary>Settings</summary>
        readonly UserSettings _settings;

        /// <summary>Current config</summary>
        Config _config;

        /// <summary>Client flavor.</summary>
        IComm _client;

        /// <summary>My logger</summary>
        readonly Logger _logger = LogManager.CreateLogger("NTerm");
        #endregion


        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        bool _forceNewline = false;
        Encoding _encoding = Encoding.UTF8;
        bool _convertToPrintable = false;
        bool _clearScreen = true;
        bool _noStatus = false;

        /// <summary>CLI prompt.</summary>
        readonly string _prompt = ">";


        // // dictionary of "special" keys with the corresponding string to send out when they are pressed
        // Dictionary<ConsoleKey, string> _specialKeys = new Dictionary<ConsoleKey, string>
        // {
        //     { ConsoleKey.UpArrow, "\x1B[A" },
        //     { ConsoleKey.DownArrow, "\x1B[B" },
        //     { ConsoleKey.RightArrow, "\x1B[C" },
        //     { ConsoleKey.LeftArrow, "\x1B[D" },
        //     { ConsoleKey.Home, "\x1B[H" },
        //     { ConsoleKey.End, "\x1B[F" },
        //     { ConsoleKey.Insert, "\x1B[2~" },
        //     { ConsoleKey.Delete, "\x1B[3~" },
        //     { ConsoleKey.PageUp, "\x1B[5~" },
        //     { ConsoleKey.PageDown, "\x1B[6~" },
        //     { ConsoleKey.F1, "\x1B[11~" },
        //     { ConsoleKey.F2, "\x1B[12~" },
        //     { ConsoleKey.F3, "\x1B[13~" },
        //     { ConsoleKey.F4, "\x1B[14~" },
        //     { ConsoleKey.F5, "\x1B[15~" },
        //     { ConsoleKey.F6, "\x1B[17~" },
        //     { ConsoleKey.F7, "\x1B[18~" },
        //     { ConsoleKey.F8, "\x1B[19~" },
        //     { ConsoleKey.F9, "\x1B[20~" },
        //     { ConsoleKey.F10, "\x1B[21~" },
        //     { ConsoleKey.F11, "\x1B[23~" },
        //     { ConsoleKey.F12, "\x1B[24~" }
        // };



        /*
        Console members

        public static (int Left, int Top) GetCursorPosition()
        public static bool CapsLock
        public static bool CursorVisible
        public static bool IsErrorRedirected
        public static bool IsInputRedirected
        public static bool IsOutputRedirected
        public static bool KeyAvailable
        public static bool NumberLock
        public static bool TreatControlCAsInput
        public static class Console
        public static ConsoleColor BackgroundColor
        public static ConsoleColor ForegroundColor
        public static ConsoleKeyInfo ReadKey()
        public static ConsoleKeyInfo ReadKey(bool intercept)
        public static Encoding InputEncoding
        public static Encoding OutputEncoding
        public static event ConsoleCancelEventHandler? CancelKeyPress
        public static int BufferHeight
        public static int BufferWidth
        public static int CursorLeft
        public static int CursorSize
        public static int CursorTop
        public static int LargestWindowHeight
        public static int LargestWindowWidth
        public static int Read()
        public static int WindowHeight
        public static int WindowLeft
        public static int WindowTop
        public static int WindowWidth
        public static Stream OpenStandardError()
        public static Stream OpenStandardInput()
        public static Stream OpenStandardOutput()
        public static string Title
        public static string? ReadLine()
        public static TextReader In
        public static TextWriter Error
        public static TextWriter Out
        public static void Beep(int frequency, int duration)
        public static void Clear()
        public static void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
        public static void ResetColor()
        public static void SetBufferSize(int width, int height)
        public static void SetCursorPosition(int left, int top)
        public static void SetError(TextWriter newError)
        public static void SetIn(TextReader newIn)
        public static void SetOut(TextWriter newOut)
        public static void SetWindowPosition(int left, int top)
        public static void SetWindowSize(int width, int height)
        public static void Write(string? value)
        public static void WriteLine(string? value)

        */


        #region Lifecycle
        /// <summary>
        /// Create the main form.
        /// </summary>
        public App()
        {
            //string appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            //_settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));


            //StartPosition = FormStartPosition.Manual;
            //Console.SetWindowPosition(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            //Console.SetWindowSize(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
            //Console. WindowHeight
            //Console. WindowLeft
            //Console. WindowTop
            //Console. WindowWidth
            //public static void SetWindowPosition(int left, int top)
            //public static void SetWindowSize(int width, int height)

            // Set up log.
            string logFileName = "log.txt";
            //string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = LogLevel.Trace;
            LogManager.MinLevelNotif = LogLevel.Trace;
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => Write(e.Message);

            // cliIn.InputEvent += CliIn_InputEvent;



            // Open config.
            var args = Environment.GetCommandLineArgs();
            if (args.Length != 2)
            {
                Write("Invalid args. Restart please.");
                //Environment.Exit(1);
            }

            try
            {
                string json = File.ReadAllText(args[1]);
                object? set = JsonSerializer.Deserialize(json, typeof(Config));
                _config = (Config)set!;

                switch (_config.Protocol.ToLower())
                {
                    case "tcp":
                        _client = new TcpComm(_config.Host, _config.Port);
                        break;

                    default:
                        _logger.Error($"Invalid protocol: {_config.Protocol}");
                        break;
                }
            }
            catch (Exception ex)
            {
                // Errors are considered fatal.
                _logger.Error($"Invalid config {args[1]}:{ex}");
            }


            // attempt to enable virtual terminal escape sequence processing
            if (!_convertToPrintable)
            {
                try
                {
                    var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

                    if (iStdOut == Defs.INVALID_HANDLE_VALUE)
                    {
                        // If the function fails, the return value is INVALID_HANDLE_VALUE.
                        // To get extended error information, call GetLastError.
                        var err = GetLastError();
                        throw new("TODO");
                    }

                    if (iStdOut == 0)
                    {
                        //If an application does not have associated standard handles, such as a service running on an
                        //interactive desktop, and has not redirected them, the return value is NULL.
                        throw new("TODO");
                    }


                    GetConsoleMode(iStdOut, out uint outConsoleMode);
                    outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                    SetConsoleMode(iStdOut, outConsoleMode);
                }
                catch
                {
                    // if the above fails, it doesn't really matter - it just means escape sequences won't process nicely
                }
            }

            Console.OutputEncoding = _encoding;

            // set up keyboard input for program control
            ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();
            Console.TreatControlCAsInput = true; // we need to use CTRL-C to activate the REPL in CircuitPython, so it can't be used to exit the application

            // // this is where data read from the serial port will be temporarily stored
            // string received = string.Empty;

        }


        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        internal void Dispose()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        //protected override void Dispose(bool disposing)
        //{
        //    _settings.FormGeometry = new Rectangle(Location.X, Location.Y, Size.Width, Size.Height);
        //    _settings.Save();


        //    if (disposing && (components != null))
        //    {
        //        _client?.Dispose();
        //        components.Dispose();
        //    }

        //    base.Dispose(disposing);
        //}
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TODO_test()
        {
            try
            {
                // Works:
                //AsyncUsingTcpClient();

                //StartServer(_config.Port);

                //var res = _client.Send("djkdjsdfksdf;s");
                //Write($"{res}: {_client.Response}");


                //var ser = new Serial();

                //var ports = ser.GetSerialPorts();



                //        public List<ComPort> GetSerialPorts()
                //        void Output(string message, bool force = false, bool newline = true, bool flush = false)


            }
            catch (Exception ex)
            {
                _logger.Error($"Fatal error: {ex.Message}");
            }
        }


        public bool Run()
        {
            bool ret = true;
            string? ucmd;

            // Check for something to do.
            if (Console.KeyAvailable)
            {
                // Console.ReadKey blocks and consumes the buffer immediately.
                var key = Console.ReadKey(false);

                if (key.KeyChar == ' ')
                {
                    // Toggle run.
                    ucmd = "r";
                    Console.WriteLine("");
                }
                else
                {
                    // Get the rest.
                    var res = Console.ReadLine();
                    ucmd = res is null ? key.KeyChar.ToString() : key.KeyChar + res;
                }

                if (ucmd is not null)
                {
                    // // Process the line. Chop up the raw command line into args.
                    // List<string> args = StringUtils.SplitByToken(ucmd, " ");

                    // // Process the command and its options.
                    // bool valid = false;
                    // if (args.Count > 0)
                    // {
                    //     foreach (var cmd in _commands!)
                    //     {
                    //         if (args[0] == cmd.LongName || (args[0].Length == 1 && args[0][0] == cmd.ShortName))
                    //         {
                    //             // Execute the command. They handle any errors internally.
                    //             valid = true;

                    //             ret = cmd.Handler(cmd, args);
                    //             break;
                    //         }
                    //     }

                    //     if (!valid)
                    //     {
                    //         Write("Invalid command");
                    //     }
                    // }
                }
                else
                {
                    // Assume finished.
//                    State.Instance.ExecState = ExecState.Exit;
                }


                //void CliIn_InputEvent(object? sender, TermInputEventArgs e)
                //{
                //    OpStatus res = OpStatus.Success;

                //    if (e.Line is not null)
                //    {
                //        _logger.Trace($"SND:{e.Line}");
                //        res = _client.Send(e.Line);
                //        e.Handled = true;
                //    }
                //    else if (e.HotKey is not null)  // single key
                //    {
                //        // If it's in the hotkeys send it now
                //        var hk = (char)e.HotKey;
                //        if (_config.HotKeys.Contains(hk))
                //        {
                //            _logger.Trace($"SND:{hk}");
                //            res = _client.Send(hk.ToString());
                //            e.Handled = true;
                //        }
                //        else
                //        {
                //            e.Handled = false;
                //        }
                //    }

                //    // Show results. TODO extract/convert ansi codes. Use regex.
                //    Write($"{res}: {_client.Response}");
                //    _logger.Trace($"RCV:{res}: {_client.Response}");
                //}



            }

            return ret;
        }

        /// <summary>
        /// Write to user. Takes care of prompt.
        /// </summary>
        /// <param name="s"></param>
        void Write(string s)
        {
            Console.WriteLine(s);
            Console.Write(_prompt);
        }



        // void Run()
        // {
        //     // this is where data read from the serial port will be temporarily stored
        //     string received = string.Empty;

        //     //main loop - keep this up until user presses CTRL-X or an exception takes us down
        //     do
        //     {
        //         // first things first, check for (and respect) a request to exit the program via CTRL-X
        //         if (Console.KeyAvailable)
        //         {
        //             keyInfo = Console.ReadKey(intercept: true);

        //             if ((keyInfo.Key == ConsoleKey.H) && (keyInfo.Modifiers == ConsoleModifiers.Control))
        //             {
        //                 ShowHelp();
        //             }

        //             if ((keyInfo.Key == ConsoleKey.X) && (keyInfo.Modifiers == ConsoleModifiers.Control))
        //             {
        //                 Output("\n<<< SimplySerial session terminated via CTRL-X >>>");
        //                 ExitProgram(silent: true);
        //             }

        //         }

        //         // this is the core functionality - loop while the serial port is open
        //         while (_serialPort.IsOpen)
        //         {
        //             try
        //             {
        //                 // process keypresses for transmission through the serial port
        //                 if (Console.KeyAvailable)
        //                 {
        //                     // determine what key is pressed (including modifiers)
        //                     keyInfo = Console.ReadKey(intercept: true);

        //                     // exit the program if CTRL-X was pressed
        //                     if ((keyInfo.Key == ConsoleKey.X) && (keyInfo.Modifiers == ConsoleModifiers.Control))
        //                     {
        //                         Output("\n<<< SimplySerial session terminated via CTRL-X >>>");
        //                         throw new();
        //                     }

        //                     // check for keys that require special processing (cursor keys, etc.)
        //                     else if (_specialKeys.ContainsKey(keyInfo.Key))
        //                         _serialPort.Write(_specialKeys[keyInfo.Key]);

        //                     // everything else just gets sent right on through
        //                     else
        //                         _serialPort.Write(Convert.ToString(keyInfo.KeyChar));
        //                 }

        //                 // process data coming in from the serial port
        //                 received = _serialPort.ReadExisting();

        //                 // if anything was received, process it
        //                 if (received.Length > 0)
        //                 {
        //                     // if we're trying to filter out title/status updates in received data, try to ensure we've got the whole string
        //                     if (_noStatus && received.Contains("\x1b"))
        //                     {
        //                         Thread.Sleep(100);
        //                         received += _serialPort.ReadExisting();
        //                     }

        //                     if (_forceNewline)
        //                         received = received.Replace("\r", "\n");

        //                     // write what was received to console
        //                     Output(received, force: true, newline: false);
        //                     start = DateTime.Now;
        //                 }
        //                 else
        //                 {
        //                     Thread.Sleep(1);
        //                 }
        //             }
        //             catch (Exception e)
        //             {

        //             }
        //         }
        //     } while (_autoConnect > AutoConnect.NONE);

        //     // if we get to this point, we should be exiting gracefully
        //     throw new("<<< SimplySerial session terminated >>>");
        // }


        /// <summary>
        /// Writes messages using Console.WriteLine() as long as the 'Quiet' option hasn't been enabled
        /// </summary>
        /// <param name="message">Message to output (assuming 'Quiet' is false)</param>
        // void Output(string message, bool force = false, bool newline = true, bool flush = false)
        // {
        //     if (!_quiet || force)
        //     {
        //         if (newline)
        //             message += "\n";

        //         if (message.Length > 0)
        //         {
        //             if (_noStatus)
        //             {
        //                 Regex r = new Regex(@"\x1b\][02];.*\x1b\\");
        //                 message = r.Replace(message, string.Empty);
        //             }

        //             if (_convertToPrintable)
        //             {
        //                 string newMessage = "";
        //                 foreach (byte c in message)
        //                 {
        //                     if ((c > 31 && c < 128) || (c == 8) || (c == 9) || (c == 10) || (c == 13))
        //                         newMessage += (char)c;
        //                     else
        //                         newMessage += $"[{c:X2}]";
        //                 }
        //                 message = newMessage;
        //             }
        //             Console.Write(message);
        //         }
        //     }
        // }
    }
}
