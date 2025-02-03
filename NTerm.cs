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

        /// <summary>User hot keys.</summary>
        readonly Dictionary<string, string> _hotKeys = [];

        /// <summary>Prompt.</summary>
        const int PROMPT_COLOR = 94;

        /// <summary>Colors.</summary>
        readonly Dictionary<LogLevel, int> _logColors = new() { { LogLevel.Error, 91 }, { LogLevel.Warn, 93 }, { LogLevel.Info, 96 }, { LogLevel.Debug, 92 } };
        #endregion

        #region Lifecycle
        /// <summary>
        /// Create the console.
        /// </summary>
        public App()
        {
            // Set up log first.
            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += LogMessage;

            LoadSettings();

            // FakeSettings();

            // WritePrompt();

            // TODO Console loc/size? https://stackoverflow.com/questions/67008500/how-to-move-c-sharp-console-application-window-to-the-center-of-the-screen
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
                        case (ConsoleModifiers.None, _):
                            // Get the rest of the line. Blocks.
                            var s = Console.ReadLine();
                            ucmd = s is null ? key.KeyChar.ToString() : key.KeyChar + s;
                            break;

                        case (ConsoleModifiers.Control, "q"):
                            running = false;
                            break;

                        case (ConsoleModifiers.Control, "s"):
                            var ed = new Editor() { Settings = _settings };
                            ed.ShowDialog();
                            LoadSettings();
                            break;

                        case (ConsoleModifiers.Control, "h"):
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
            //// Save user settings. _settings.FormGeometry = 

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
            // Console.Write(s + Environment.NewLine);
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
