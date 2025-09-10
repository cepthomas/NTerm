using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


namespace NTerm
{
    public class App : IDisposable
    {
        #region Fields
        /// <summary>My logger</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>Settings</summary>
        readonly UserSettings _settings = new();

        /// <summary>Current config</summary>
        Config? _config = null;

        /// <summary>Client comm flavor.</summary>
        IComm? _comm = null;

        /// <summary>User hot keys.</summary>
        readonly Dictionary<char, string> _hotKeys = [];

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<CliInput> _qCli = new();

        /// <summary>For timing measurements.</summary>
        readonly TimeIt _tmit = new();
        #endregion

        /// <summary>Build me one.</summary>
        public App()
        {
            //TimeIt.Snap("App()");

            // Set up log first.
            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => { Print($"{e.Message}"); DoPrompt(); };
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            var ok = InitFromSettings();

            if (ok && _config is not null)
            {
                Run(); // forever
            }

            // All done.
            _settings.Save();
            LogManager.Stop();
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            // Console.WriteLine("====== Dispose !!!! ======");
            _comm?.Dispose();
            _comm = null;
        }

        /// <summary>
        /// Main loop.
        /// </summary>
        public void Run()
        {
            DoPrompt();

            using CancellationTokenSource ts = new();
            using Task taskKeyboard = Task.Run(() => DoKeyboard(ts.Token));

            bool timedOut = false;

            while (!ts.Token.IsCancellationRequested)
            {
                timedOut = false;

                //=========== CLI input? ============//
                if (_qCli.TryDequeue(out CliInput? le))
                {
                    // Check for meta key.
                    if (le.Text.Length > 1 && le.Text.StartsWith(_settings.MetaMarker))
                    {
                        switch (le.Text[1])
                        {
                            case 'q':
                                ts.Cancel();
                                break;

                            case 's':
                                SettingsEditor.Edit(_settings, "NTerm", 500);
                                InitFromSettings();
                                break;

                            case '?':
                                Help();
                                break;

                            default:
                                _logger.Warn($"Unknown meta key: {le.Text[1]}");
                                break;
                        }
                    }
                    else
                    {
                        // var start = Utils.GetCurrentMsec();
                        var (stat, msg, resp) = _comm.Send(le.Text); // do something? TODO1 UDP can't wait for user input - should poll anyways.
                        // _logger.Debug($"Send took {Utils.GetCurrentMsec() - start}");

                        switch (stat)
                        {
                            case OpStatus.Success:
                                Print(resp);
                                break;

                            case OpStatus.Error:
                                _logger.Error($"Comm.Send() error [{msg}]");
                                break;

                            case OpStatus.ConnectTimeout:
                                timedOut = true;
                                Print("Server not connecting");
                                break;

                            case OpStatus.ResponseTimeout:
                                timedOut = true;
                                Print("Server not responding");
                                break;
                        }
                    }

                    DoPrompt();
                }

                // If there was no timeout, delay a bit.
                if (!timedOut) Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Service the cli read.
        /// </summary>
        /// <param name="token"></param>
        void DoKeyboard(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // Check for something to do.
                if (Console.KeyAvailable)
                {
                    var conkey = Console.ReadKey(false);
                    char key = conkey.KeyChar;
                    //Console.WriteLine($">>>{key}");

                    // Check for hot key.
                    if (MatchMods(_settings.HotKeyMod, conkey.Modifiers))
                    {
                        _qCli.Enqueue(new(_hotKeys[(char)conkey.Key], conkey.Modifiers));
                    }
                    else // Ordinary, get the rest - blocks.
                    {
                        var rest = Console.ReadLine();
                        _qCli.Enqueue(new(key + rest, conkey.Modifiers));
                    }
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        /// <summary>
        /// Write to console.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="nl">Default is to add a nl.</param>
        void Print(string text, bool nl = true)
        {
            bool hasMatch = false;
            foreach (Matcher m in _config.Matchers)
            {
                if (text.Contains(m.Text))
                {
                    if (m.ForeColor is not ConsoleColorEx.None) { Console.ForegroundColor = (ConsoleColor)m.ForeColor; }
                    if (m.BackColor is not ConsoleColorEx.None) { Console.BackgroundColor = (ConsoleColor)m.BackColor; }
                    Console.Write(text);
                    Console.ResetColor();
                    hasMatch = true;
                    break;
                }
            }

            if (!hasMatch)
            {
                Console.Write(text);
            }

            if (nl)
            {
                Console.Write(Environment.NewLine);
            }
        }

        /// <summary>
        /// Load persisted settings.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        bool InitFromSettings()
        {
            bool ok = true;

            // Reset.
            _config = null;
            _comm?.Dispose();
            _comm = null;

            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;

            var lc = _settings.Configs.Select(x => x.Name).ToList();

            if (lc.Count > 0)
            {
                var c = _settings.Configs.FirstOrDefault(x => x.Name == _settings.CurrentConfig);
                _config = c is null ? _settings.Configs[0] : c;

                try
                {
                    _comm = _config.CommType switch
                    {
                        CommType.Null => new NullComm(_config),
                        CommType.Tcp => new TcpComm(_config),
                        CommType.Serial => new SerialComm(_config),
                        _ => throw new NotImplementedException(),
                    };
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message);
                    ok = false;
                    return ok;
                }

                // Init hotkeys.
                _hotKeys.Clear();
                _config.HotKeys.ForEach(hk =>
                {
                    var parts = hk.SplitByToken("=");

                    if (parts.Count == 2 && parts[0].Length == 1 && parts[1].Length > 0)
                    {
                        _hotKeys[parts[0].ToUpper()[0]] = parts[1];
                    }
                    else
                    {
                        _logger.Warn($"Invalid hotkey:{hk}");
                    }
                });

                _logger.Info($"Using {_config.Name} [{_config.CommType}]");
            }

            if (_config is null)
            {
                _logger.Warn($"Client is not selected - edit your settings");
                _comm?.Dispose();
                _comm = null;
                _config = null;
                ok = false;
            }
            return ok;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyMod"></param>
        /// <param name="consMods"></param>
        /// <returns></returns>
        bool MatchMods(KeyMod keyMod, ConsoleModifiers consMods)
        {
            bool match = false;

            switch (keyMod, consMods)
            {
                case (KeyMod.Ctrl, ConsoleModifiers.Control):
                case (KeyMod.Alt, ConsoleModifiers.Alt):
                case (KeyMod.Shift, ConsoleModifiers.Shift):
                case (KeyMod.CtrlShift, ConsoleModifiers.Control | ConsoleModifiers.Shift):
                    match = true;
                    break;
            }

            return match;
        }

        /// <summary>
        /// 
        /// </summary>
        void Help()
        {
            var cc = _config is not null ? $"{_config.Name}({_config.CommType})" : "None";
            Print($"current config: {cc}");

            _hotKeys.ForEach(x => Print($"{_settings.HotKeyMod}-{x.Key} sends: [{x.Value}]"));

            Print("serial ports:");
            var sports = SerialPort.GetPortNames();
            sports.ForEach(s => { Print($"   {s}"); });
        }

        /// <summary>
        /// 
        /// </summary>
        void DoPrompt()
        {
            Console.Write(_settings.Prompt);
        }
    }
}
