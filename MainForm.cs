using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;


namespace NTerm
{
    public partial class MainForm : Form
    {
        #region Fields App
        /// <summary>My logger</summary>
        readonly Logger _logger = LogManager.CreateLogger("NTerm");

        /// <summary>Settings</summary>
        UserSettings _settings = new();

        /// <summary>Current config</summary>
        Config? _config = null;

        /// <summary>Client flavor.</summary>
        IComm? _comm = null;

        /// <summary>User hot keys.</summary>
        readonly Dictionary<string, string> _hotKeys = [];

        /// <summary>Prompt.</summary>
        const int PROMPT_COLOR = 94;

        /// <summary>Colors.</summary>
        readonly Dictionary<LogLevel, int> _logColors = new() { { LogLevel.Error, 91 }, { LogLevel.Warn, 93 }, { LogLevel.Info, 96 }, { LogLevel.Debug, 92 } };
        #endregion



        public MainForm()
        {
            InitializeComponent();

            // Set up log first.
            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += LogMessage;

            LoadSettings();

            // string appDir = MiscUtils.GetAppDataDir("TODO", "Ephemera");
            // _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            StartPosition = FormStartPosition.Manual;
            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);

            // // Set up log.
            // string logFileName = Path.Combine(appDir, "log.txt");
            // LogManager.MinLevelFile = LogLevel.Trace;
            // LogManager.MinLevelNotif = LogLevel.Trace;
            // LogManager.Run(logFileName, 50000);

            // LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => tvOut.AppendLine(e.Message);

            cliIn.InputEvent += CliInput_InputEvent;

            //btnGo.Click += BtnGo_Click;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // // Open config.
            // var fn = @"C:\Dev\WinFormsApp1\config.json";

            // try
            // {
            //     string json = File.ReadAllText(fn);
            //     object? set = JsonSerializer.Deserialize(json, typeof(Config));
            //     _config = (Config)set!;

            //     switch (_config.Protocol.ToLower())
            //     {
            //         case "tcp":
            //             _prot = new TcpProtocol(_config.Host, _config.Port);
            //             break;

            //         default:
            //             _logger.Error($"Invalid protocol: {_config.Protocol}");
            //             break;
            //     }
            // }
            // catch (Exception ex)
            // {
            //     // Errors are considered fatal.
            //     _logger.Debug($"Invalid config {fn}:{ex}");
            // }

            base.OnLoad(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {

            // _settings.Save();

            SaveSettings();

            _comm?.Dispose();
            _comm = null;

            base.OnFormClosing(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BtnGo_Click(object? sender, EventArgs e)
        {
            try
            {
                // Works:
                //AsyncUsingTcpClient();

                //StartServer(_config.Port);
                //var res = _prot.Send("djkdjsdfksdf;s");
                //tvOut.AppendLine(res);

            }
            catch (Exception ex)
            {
                _logger.Error($"Fatal error: {ex.Message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CliInput_InputEvent(object? sender, CliInputEventArgs e)
        {
            string? res = null;

            if (e.Line is not null)
            {
                _logger.Trace($"SND:{e.Line}");
//                res = _prot.Send(e.Line);
                e.Handled = true;
            }
            else if (e.HotKey is not null)  // single key
            {
                // If it's in the hotkeys send it now
                //var hk = (char)e.HotKey;
                //if (_config.HotKeys.Contains(hk))
                //{
                //    _logger.Trace($"SND:{hk}");
                //    res = _prot.Send(hk.ToString());
                //    e.Handled = true;
                //}
                //else
                //{
                //    e.Handled = false;
                //}
            }

            //var s = res ?? "NULL";

            _logger.Trace($"RCV:{res ?? "NULL"}");
        }

        /// <summary>
        /// Run loop forever.
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            bool running = true;
            string? ucmd = null;
            OpStatus res;
            bool ok = true;


            //https://github.com/jrnker/Proxmea.ConsoleHelper


            //https://github.com/microsoft/referencesource/blob/master/mscorlib/system/console.cs

            while (running)
            {
                // Check for something to do. Get the first character in user input.
                if (Console.KeyAvailable)
                {
                    //_logger.Trace($">>> key available");

                    var key = Console.ReadKey(false);

                    if (key.Key == ConsoleKey.Enter)
                    {
                        int y = Console.CursorTop;
                        int x = Console.CursorLeft;

                        Console.SetCursorPosition(0, Console.CursorTop); // x, y

                        Console.Write(new string(' ', Console.WindowWidth));

                        Console.SetCursorPosition(0, y);


                    }

                    if (key.Modifiers == ConsoleModifiers.Alt)
                    {


                    }

                    //public static void ClearCurrentConsoleLine()
                    //{
                        //int currentLineCursor = Console.CursorTop;
                        //Console.SetCursorPosition(0, Console.CursorTop);
                        //Console.Write(new string(' ', Console.WindowWidth)); 
                        //Console.SetCursorPosition(0, currentLineCursor);
                    //}

                    //Console.Write(key.KeyChar.ToString().ToUpper());

                    var lkey = (key.Modifiers & ConsoleModifiers.Shift) > 0 ? key.Key.ToString() : key.Key.ToString().ToLower();

                    _logger.Trace($">>> read key.Key:{key.Key} key.KeyChar:{key.KeyChar} mod:{key.Modifiers} {(int)key.Modifiers}");
                    // 2025-02-04 16:08:08.050 TRC NTerm App.cs(128) >>> read key.Key:A key.KeyChar:a mod:None
                    // 2025-02-04 16:08:19.603 TRC NTerm App.cs(128) >>> read key.Key:A key.KeyChar: mod:Control


                    ok = true;

                    switch (key.Modifiers, lkey)
                    {
                        case (ConsoleModifiers.None, _):
                            // Get the rest of the line. Blocks.
                            var s = Console.ReadLine();
                            _logger.Trace($">>> got line:{s}");
                            ucmd = s is null ? lkey : lkey + s;
                            break;

                        case (ConsoleModifiers.Alt, "q"):
                            running = false;
                            break;

                        case (ConsoleModifiers.Alt, "s"):
                            var ed = new Editor() { Settings = _settings };
                            ed.ShowDialog();
                            LoadSettings();
                            break;

                        case (ConsoleModifiers.Alt, "h"):
                            Help();
                            break;

                        case (ConsoleModifiers.Alt, _):
                            ok = _hotKeys.TryGetValue(lkey, out ucmd);
                            break;

                        default:
                            ok = false;
                            break;
                    }

                    if (!ok)
                    {
                        Write("Invalid command");
                        // WritePrompt();
                    }
                    else if (ucmd is not null)
                    {
                        if (_comm is null)
                        {
                            _logger.Warn($"Comm is not initialized - edit settings");
                        }
                        else
                        {
                            _logger.Trace($"SND:{ucmd}");
                            res = _comm.Send(ucmd);
                            // Show results.
                            _logger.Trace($"RCV:{res}: {_comm.Response}");
                        }
                        // WritePrompt();
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }

            return ok;
        }

        #region Settings
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        void LoadSettings()
        {
            // Reset.
            _config = null;
            _comm?.Dispose();
            _comm = null;

            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;

            var lc = _settings.Configs.Select(x => x.Name).ToList();

            if (lc.Count > 0)
            {
                _config = _settings.Configs[0];
                OpStatus stat = OpStatus.Success;

                _comm = _config.CommType switch
                {
                    CommType.Tcp => new TcpComm(),
                    CommType.Serial => new SerialComm(),
                    CommType.Null => new NullComm(),
                    _ => null
                };

                // Init and check stat.
                stat = _comm.Init(_config.Args);

                // Init hotkeys.
                _hotKeys.Clear();
                _config.HotKeys.ForEach(hk =>
                {
                    var parts = hk.SplitByToken("=", false); // respect intentional spaces

                    if (parts.Count == 2 && parts[0].Length == 1 && parts[1].Length > 0)
                    {
                        _hotKeys[parts[0]] = parts[1];
                    }
                    else
                    {
                        _logger.Warn($"Invalid hotkey:{hk}");
                    }
                });

                _logger.Info($"NTerm using {_config.Name}({_config.CommType})");
                return;
            }

            _comm?.Dispose();
            _comm = null;
            _config = null;
            _logger.Warn($"Comm is not initialized - edit settings");
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveSettings()
        {
            _settings.FormGeometry = new Rectangle(Location.X, Location.Y, Size.Width, Size.Height);

            _settings.Save();
        }
        #endregion

        #region Misc
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogMessage(object? sender, LogMessageEventArgs e)
        {
            if (_settings.AnsiColor)
            {
                _logColors.TryGetValue(e.Level, out int color);
                Write($"\u001b[{color}m{e.Message}\u001b[0m");
            }
            else
            {
                Write(e.Message);
            }
        }

        /// <summary>
        /// Write to user.
        /// </summary>
        /// <param name="s"></param>
        void Write(string s)
        {
            Console.WriteLine(s);
        }

        /// <summary>
        /// Write prompt. TODO useful? Blinking cursor is probably adequate.
        /// </summary>
        void WritePrompt()
        {
            if (_settings.AnsiColor)
            {
                Console.Write($"\u001b[{PROMPT_COLOR}m{_settings.Prompt}\u001b[0m");
            }
            else
            {
                Console.Write(_settings.Prompt);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Help()
        {
            Write("ctrl-s to edit settings");
            Write("ctrl-q to exit");
            Write("ctrl-h this here");

            _hotKeys.ForEach(x => Write($"alt-{x.Key} send {x.Value}"));

            var cc = _config is not null ? $"{_config.Name}({_config.CommType})" : "None";
            Write($"current config: {cc}");
        }
        #endregion

    }
}
