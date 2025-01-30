using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>My logger</summary>
        readonly Logger _logger = LogManager.CreateLogger("NTerm");

        /// <summary>Settings</summary>
        readonly UserSettings _settings;

        /// <summary>Current config</summary>
        readonly Config _config = new();

        /// <summary>Client flavor.</summary>
        readonly IComm _comm = new NullComm();
        #endregion


#if FUNNY_STUFF
        // NativeMethods.
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
        // bool _convertToPrintable = false;

         dictionary of "special" keys with the corresponding string to send out when they are pressed
         https://en.wikipedia.org/wiki/ANSI_escape_code
         Dictionary<ConsoleKey, string> _specialKeys = new Dictionary<ConsoleKey, string>
         {
             { ConsoleKey.UpArrow, "\x1B[A" },
             { ConsoleKey.DownArrow, "\x1B[B" },
             { ConsoleKey.RightArrow, "\x1B[C" },
             { ConsoleKey.LeftArrow, "\x1B[D" },
             { ConsoleKey.Home, "\x1B[H" },
             { ConsoleKey.End, "\x1B[F" },
             { ConsoleKey.Insert, "\x1B[2~" },
             { ConsoleKey.Delete, "\x1B[3~" },
             { ConsoleKey.PageUp, "\x1B[5~" },
             { ConsoleKey.PageDown, "\x1B[6~" },
             { ConsoleKey.F1, "\x1B[11~" },
             { ConsoleKey.F2, "\x1B[12~" },
             { ConsoleKey.F3, "\x1B[13~" },
             { ConsoleKey.F4, "\x1B[14~" },
             { ConsoleKey.F5, "\x1B[15~" },
             { ConsoleKey.F6, "\x1B[17~" },
             { ConsoleKey.F7, "\x1B[18~" },
             { ConsoleKey.F8, "\x1B[19~" },
             { ConsoleKey.F9, "\x1B[20~" },
             { ConsoleKey.F10, "\x1B[21~" },
             { ConsoleKey.F11, "\x1B[23~" },
             { ConsoleKey.F12, "\x1B[24~" }
         };
#endif



        /// <summary>
        /// Create the main form.
        /// </summary>
        public App()
        {
            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            Console.SetWindowPosition(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Console.SetWindowSize(_settings.FormGeometry.Width, _settings.FormGeometry.Height);

            // Set up log.
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = LogLevel.Trace;
            LogManager.MinLevelNotif = LogLevel.Trace;
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => Write(e.Message);

#if FUNNY_STUFF
             TODO??? attempt to enable virtual terminal escape sequence processing
             https://learn.microsoft.com/en-us/windows/console/setconsolemodec
             https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences
             if (!_convertToPrintable)
             {
                 var hnd = GetStdHandle(STD_OUTPUT_HANDLE);

                 if (hnd == Defs.INVALID_HANDLE_VALUE)
                 {
                     // If the function fails, the return value is INVALID_HANDLE_VALUE.
                     // To get extended error information, call GetLastError.
                     var err = GetLastError();
                     throw new("TODO");
                 }

                 if (hnd == 0)
                 {
                     //If an application does not have associated standard handles, such as a service running on an
                     //interactive desktop, and has not redirected them, the return value is NULL.
                     throw new("TODO");
                 }


                 GetConsoleMode(hnd, out uint mode);
                 mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                 SetConsoleMode(hnd, mode);

                 // if the above fails, it doesn't really matter - it just means escape sequences won't process nicely
             }


            // set up keyboard input for program control
            ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();
            Console.TreatControlCAsInput = true; // TODO we need to use CTRL-C to activate the REPL in CircuitPython,
                                                 // so it can't be used to exit the application
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveSettings()
        {
            // Save user settings.
            _settings.FormGeometry = new()
            {
                X = Console.WindowLeft,
                Y = Console.WindowTop,
                Width = Console.WindowWidth,
                Height = Console.WindowHeight
            };

            _settings.Save();
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            SaveSettings();
        }

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
                //var res = _comm.Send("djkdjsdfksdf;s");
                //Write($"{res}: {_comm.Response}");
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



        void OpenConfig(string which) // TODO
        {
            //Console.OutputEncoding = _encoding;

            // try
            // {
            //     string json = File.ReadAllText(args[1]);
            //     object? set = JsonSerializer.Deserialize(json, typeof(Config));
            //     _config = (Config)set!;

            //     switch (_config.Protocol.ToLower())
            //     {
            //         case "tcp":
            //             _comm = new TcpComm(_config.Host, _config.Port);
            //             break;

            //         default:
            //             _logger.Error($"Invalid protocol: {_config.Protocol}");
            //             break;
            //     }
            // }
            // catch (Exception ex)
            // {
            //     // Errors are considered fatal.
            //     _logger.Error($"Invalid config {args[1]}:{ex}");
            // }
        }

        /// <summary>
        /// Run loop forever.
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            bool ok = true;
            string? ucmd = null;
            OpStatus res = OpStatus.Success;

            while (ok)
            {
                // Check for something to do.
                if (Console.KeyAvailable)
                {
                    // Console.ReadKey blocks and consumes the buffer immediately.
                    var key = Console.ReadKey(false);


                    // public char KeyChar
                    // 
                    // public ConsoleKey Key
                    // None = 0,
                    // Backspace = 8,
                    // Tab = 9,
                    // Clear = 12,
                    // Enter = 13,
                    // Escape = 27,
                    // Spacebar = 32,
                    // PageUp = 33,
                    // Home = 36,
                    // LeftArrow = 37,
                    // The 1 key.
                    // D1 = 49,
                    // A = 65,
                    // 
                    // public ConsoleModifiers Modifiers
                    // Alt = 1,
                    // Shift = 2,
                    // Control = 4
                    var lkey = key.KeyChar.ToString().ToLower();

                    switch (key.Modifiers, lkey)
                    {
                        case (ConsoleModifiers.None, _):
                            // Get the rest of the line. Blocks.
                            var s = Console.ReadLine();
                            ucmd = s is null ? key.KeyChar.ToString() : key.KeyChar + s;
                            _logger.Trace($"SND:{ucmd}");
                            res = _comm.Send(ucmd);
                            // Show results.
                            Write($"{res}: {_comm.Response}");
                            _logger.Trace($"RCV:{res}: {_comm.Response}");
                            break;

                        // Commands.
                        case (ConsoleModifiers.Control, "q"):
                            // Quit
                            Environment.Exit(0);
                            break;

                        case (ConsoleModifiers.Control, "s"):
                            // Settings
                            EditSettings();
                            break;

                        case (ConsoleModifiers.Control, "c"):
                            // Settings
                            EditSettings();
                            break;

                        case (ConsoleModifiers.Control, "?"):
                            // Help TODO
                            break;

                        // ctrl: ?=help, q=exit/quit, s=edit/settings, list configs, select config

                        case (ConsoleModifiers.Alt, _):
                            // Hotkeys.
                            if (_config.HotKeys.Contains(lkey))
                            {
                                // TODO something.
                                //   custom, alt-z="ababab" ...
                            }
                            break;

                        default:
                            //Write("Invalid command");
                            break;
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }

            return ok;
        }

        /// <summary>
        /// Write to user. Takes care of prompt.
        /// </summary>
        /// <param name="s"></param>
        void Write(string s)
        {
            Console.WriteLine(s);
            Console.Write(_settings.Prompt);
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void EditSettings()
        {
            PropertyGrid pg = new()
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.Categorized,
                SelectedObject = _settings
            };

            //if (settings is SettingsCore)
            //{
            //    pg.AddButton("Clear recent", null, "Clear recent file list", (_, __) => (settings as SettingsCore)!.RecentFiles.Clear());
            //}

            using Form f = new()
            {
                Text = "User Settings",
                AutoScaleMode = AutoScaleMode.None,
                Location = Cursor.Position,
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                ShowIcon = false,
                ShowInTaskbar = false
            };
            f.ClientSize = new(450, 500); // do after construction

            // Detect changes of interest.
            bool restart = false;
            pg.PropertyValueChanged += (sdr, args) =>
            {
                var name = args.ChangedItem!.PropertyDescriptor!.Name;
                var cat = args.ChangedItem!.PropertyDescriptor!.Category;
                restart = true;
            };

            pg.ExpandAllGridItems();
            f.Controls.Add(pg);
            f.ShowDialog();

            if (restart)
            {
                SaveSettings();
                MessageBox.Show("Restart required for device changes to take effect");
            }
        }


#if FUNNY_STUFF
         void Run()
         {
             // this is where data read from the serial port will be temporarily stored
             string received = string.Empty;

             //main loop - keep this up until user presses CTRL-X or an exception takes us down
             do
             {
                 // first things first, check for (and respect) a request to exit the program via CTRL-X
                 if (Console.KeyAvailable)
                 {
                     keyInfo = Console.ReadKey(intercept: true);

                     if ((keyInfo.Key == ConsoleKey.H) && (keyInfo.Modifiers == ConsoleModifiers.Control))
                     {
                         ShowHelp();
                     }

                     if ((keyInfo.Key == ConsoleKey.X) && (keyInfo.Modifiers == ConsoleModifiers.Control))
                     {
                         Output("\n<<< SimplySerial session terminated via CTRL-X >>>");
                         ExitProgram(silent: true);
                     }

                 }

                 // this is the core functionality - loop while the serial port is open
                 while (_serialPort.IsOpen)
                 {
                     try
                     {
                         // process keypresses for transmission through the serial port
                         if (Console.KeyAvailable)
                         {
                             // determine what key is pressed (including modifiers)
                             keyInfo = Console.ReadKey(intercept: true);

                             // exit the program if CTRL-X was pressed
                             if ((keyInfo.Key == ConsoleKey.X) && (keyInfo.Modifiers == ConsoleModifiers.Control))
                             {
                                 Output("\n<<< SimplySerial session terminated via CTRL-X >>>");
                                 throw new();
                             }

                             // check for keys that require special processing (cursor keys, etc.)
                             else if (_specialKeys.ContainsKey(keyInfo.Key))
                                 _serialPort.Write(_specialKeys[keyInfo.Key]);

                             // everything else just gets sent right on through
                             else
                                 _serialPort.Write(Convert.ToString(keyInfo.KeyChar));
                         }

                         // process data coming in from the serial port
                         received = _serialPort.ReadExisting();

                         // if anything was received, process it
                         if (received.Length > 0)
                         {
                             // if we're trying to filter out title/status updates in received data, try to ensure we've got the whole string
                             if (_noStatus && received.Contains("\x1b"))
                             {
                                 Thread.Sleep(100);
                                 received += _serialPort.ReadExisting();
                             }

                             if (_forceNewline)
                                 received = received.Replace("\r", "\n");

                             // write what was received to console
                             Output(received, force: true, newline: false);
                             start = DateTime.Now;
                         }
                         else
                         {
                             Thread.Sleep(1);
                         }
                     }
                     catch (Exception e)
                     {

                     }
                 }
             } while (_autoConnect > AutoConnect.NONE);

             // if we get to this point, we should be exiting gracefully
             throw new("<<< SimplySerial session terminated >>>");
         }


        / <summary>
        / Writes messages using Console.WriteLine() as long as the 'Quiet' option hasn't been enabled
        / </summary>
        / <param name="message">Message to output (assuming 'Quiet' is false)</param>
         void Output(string message, bool force = false, bool newline = true, bool flush = false)
         {
             if (!_quiet || force)
             {
                 if (newline)
                     message += "\n";

                 if (message.Length > 0)
                 {
                     if (_noStatus)
                     {
                         Regex r = new Regex(@"\x1b\][02];.*\x1b\\");
                         message = r.Replace(message, string.Empty);
                     }

                     if (_convertToPrintable)
                     {
                         string newMessage = "";
                         foreach (byte c in message)
                         {
                             if ((c > 31 && c < 128) || (c == 8) || (c == 9) || (c == 10) || (c == 13))
                                 newMessage += (char)c;
                             else
                                 newMessage += $"[{c:X2}]";
                         }
                         message = newMessage;
                     }
                     Console.Write(message);
                 }
             }
         }
#endif
    }
}
