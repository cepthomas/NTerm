using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        UserSettings _settings = new();

        /// <summary>Current config</summary>
        Config? _config = null;

        /// <summary>Client flavor.</summary>
        IComm? _comm = null;
        #endregion


        Dictionary<string, string> _hotKeys = [];

        const int PROMPT_COLOR = 94;

        Dictionary<LogLevel, int> _logColors = new() { { LogLevel.Error, 91 }, { LogLevel.Warn, 93 }, { LogLevel.Info, 96 }, { LogLevel.Debug, 92 } };


        #region Lifecycle
        /// <summary>
        /// Create the console.
        /// </summary>
        public App()
        {
            LoadSettings();

            DoFake();

            // Set up log.
            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += LogMessage;

            //TODO Loc/size? https://stackoverflow.com/questions/67008500/how-to-move-c-sharp-console-application-window-to-the-center-of-the-screen
            //not: Console.SetWindowPosition(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            //     Console.SetWindowSize(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            SaveSettings();
            _comm?.Dispose();
            _comm = null;
        }
        #endregion

        void DoFake()
        {
            UserSettings ss = new();
            Random rnd = new();

            ss.Prompt = "!!!";
            ss.FormGeometry = new(200, 200, 500, 500);

            for (int i = 0; i < 5; i++)
            {
                var c = new Config()
                {
                    Args = $"arg{i}",
                    Name = $"name{i}",
                };

                for (int j = 0; j < 3; j++)
                {
                    c.HotKeys.Add($"hk{j}={i}bla bla{j}");
                }
                ss.Configs.Add(c);

                //ss.CurrentConfig.Add(c.Name);
            }

            ss.CurrentConfig = "";
            var ed = new Editor() { Settings = ss };
            ed.ShowDialog();
            LoadSettings();
            
            
            // Works:
            //AsyncUsingTcpClient();
            //StartServer(_config.Port);
            //var res = _comm.Send("djkdjsdfksdf;s");
            //Write($"{res}: {_comm.Response}");
            //var ser = new Serial();
            //var ports = ser.GetSerialPorts();



            //public List<ComPort> GetSerialPorts()
            //void Output(string message, bool force = false, bool newline = true, bool flush = false)
        }

        /// <summary>
        /// Run loop forever.
        /// </summary>
        /// <returns></returns>
        public bool Run()
        {
            bool running = true;
            string? ucmd = null;
            OpStatus res = OpStatus.Success;
            bool ok = true;

            while (running)
            {
                // Check for something to do. Get the first character in user input.
                if (Console.KeyAvailable)
                {
                    ok = true;
                    var key = Console.ReadKey(false);
                    var lkey = key.Key.ToString().ToLower();

                    switch (key.Modifiers, lkey)
                    {
                        case (ConsoleModifiers.None, _): // Get the rest of the line. Blocks.
                            var s = Console.ReadLine();
                            ucmd = s is null ? key.KeyChar.ToString() : key.KeyChar + s;
                            break;

                        case (ConsoleModifiers.Control, "q"): // Controlkey?
                            running = false;
                            break;

                        case (ConsoleModifiers.Control, "s"): // Controlkey?
                            var ed = new Editor() { Settings = _settings };
                            ed.ShowDialog();
                            LoadSettings();
                            break;

                        case (ConsoleModifiers.Control, "?"):
                            Usage();
                            break;

                        case (ConsoleModifiers.Alt, _): // Hotkey?
                            ok = _hotKeys.TryGetValue(lkey, out ucmd);
                            break;

                        default:
                            ok = false;
                            break;
                    }

                    if (!ok)
                    {
                        Write("Invalid command");
                    }
                    else if (ucmd is not null)
                    {
                        _logger.Trace($"SND:{ucmd}");
                        res = _comm.Send(ucmd);
                        // Show results.
                        Write($"{res}: {_comm.Response}");
                        _logger.Trace($"RCV:{res}: {_comm.Response}");
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
                    //_ => _logger.Error($"Invalid comm type:{_config.CommType}")
                };

                // Init and check stat.
                stat = _comm.Init(_config.Args);

                // Init hotkeys.
                _hotKeys.Clear();
                _config.HotKeys.ForEach(hk =>
                {
                    var parts = hk.SplitByToken("=", false); // leave intentional spaces
                    if (parts.Count == 2)
                    {
                        _hotKeys[parts[0]] = parts[1];
                    }
                    else
                    {
                        _logger.Warn($"Invalid hotkey:{hk}");
                    }
                });

            }
            else
            {
                _config = null;
                _comm = null;
                _logger.Warn("TODO select a config!");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SaveSettings()
        {
            //// Save user settings.
            //_settingsX.FormGeometry = new()
            //{
            //    X = Console.WindowLeft,
            //    Y = Console.WindowTop,
            //    Width = Console.WindowWidth,
            //    Height = Console.WindowHeight
            //};

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
                Write($"\033[{color}m{e.Message}\033[0m");
            }
            else
            {
                Write(e.Message);
            }
        }

        /// <summary>
        /// Write to user. Takes care of prompt.
        /// </summary>
        /// <param name="s"></param>
        void Write(string s)
        {
            Console.WriteLine(s);

            if (_settings.AnsiColor)
            {
                Console.Write($"\033[{PROMPT_COLOR}m{_settings.Prompt}\033[0m");
            }
            else
            {
                Console.Write(_settings.Prompt);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        void Usage()
        {
            Write("TODO");
        }
        #endregion
    }
}
