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
using Ephemera.NBagOfTricks.Slog;
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
        double _startMsec;
        #endregion




        // // Human polling time in msec.
        // readonly int loop_time = 50;
        // // Server must reply to client in msec or it's considered dead.
        // readonly int server_response_time = 200;  // 100?
        // // Last command time. Non zero implies waiting for a response.
        // long sendts = 0;



        #region Lifecycle
        /// <summary>Build me one.</summary>
        public App()
        {
            GetCurrentMsec(true);

            // Set up log first.
            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => Write($"{e.Message}");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            InitFromSettings();

            if (_config is not null)
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
            Console.WriteLine("====== Dispose !!!! ======");
            _comm?.Dispose();
            _comm = null;
        }
        #endregion

        #region Main loop
        /// <summary>
        /// Main loop.
        /// </summary>
        public void Run()
        {
            // TODO write prompt somewhere/when?

            // TODO sanity check.
            Write(">>>[38;2;204;39;187mYou have freedom here[0m.\n<NL>The only guide\r\n<CR><NL>is your heart.");

            using CancellationTokenSource ts = new();

            using Task taskKeyboard = Task.Run(() => DoKeyboard(ts.Token));
            // Task taskRead = Task.Run(() => DoRead(ts.Token));

            bool timedOut = false;

            while (!ts.Token.IsCancellationRequested)
            {
                timedOut = false;

                //=========== CLI input? ============//
                if (_qCli.TryDequeue(out CliInput? le))
                {
                    // Check meta key.
                    if (MatchMods(_settings.MetaKeyMod, le.Modifiers))
                    {
                        switch (le.Text)
                        {
                            case "q":
                                ts.Cancel();
                                break;

                            case "s":
                                /*var eds =*/
                                SettingsEditor.Edit(_settings, "NTerm", 120);
                                InitFromSettings();
                                break;

                            case "?":
                                Help();
                                break;

                            default:
                                _logger.Warn($"Unknown meta key: {le.Text}");
                                break;
                        }
                    }
                    else
                    {
                        // Measure round trip for timeout.
                        var start = GetCurrentMsec();
                        var res = _comm.Send(le.Text); // do something?
                        _logger.Debug($"Send took {GetCurrentMsec() - start}");
                        switch (res.stat)
                        {
                            case OpStatus.Success:
                                break;

                            case OpStatus.Error:
                                _logger.Error($"Comm.Send() error [{res.msg}]");
                                break;

                            case OpStatus.Timeout:
                                timedOut = true;
                                break;
                        }

                    }
                }

                //=========== Comm input? ============//
                {
                    var start = GetCurrentMsec();
                    var res = _comm.Receive();
                    _logger.Debug($"Receive took {GetCurrentMsec() - start}");

                    switch (res.stat)
                    {
                        case OpStatus.Success:
                            Write(res.data);
                            break;

                        case OpStatus.Error:
                            _logger.Error($"Comm.Receive() error [{res.msg}]");
                            break;

                        case OpStatus.Timeout:
                            timedOut = true;
                            break;
                    }
                }




                // if (_qCommRead.TryDequeue(out string data))
                // {
                //     // TODO Fix line endings??
                //     // data = data.Replace("\n", "\r\n").Replace("\r\r", "");
                //     Write(data);
                // }

                /*/ ##### Get any server responses. #####
                if self.commif is not None:
                    try:
                        # Don't block.
                        self.sock.settimeout(0)

                        done = False
                        while not done:
                            s = self.commif.read(100)
                            if s == '':
                                done = True
                            else:
                                sys.stdout.write(s)
                                sys.stdout.flush()
                                # Reset watchdog.
                                self.sendts = 0
                    except TimeoutError:
                        # Nothing to read.
                        timed_out = True
                        Reset()
                    except ConnectionError:
                        # Server disconnected.
                        Reset()
                    except Exception as e:
                        # Other unexpected errors.
                        self.do_error(e)
                */


                // Check for server not responding but still connected.
                //if self.commif is not None and self.sendts > 0:
                //    dur = get_current_msec() - self.sendts
                //    if dur > self.server_response_time:
                //        self.do_info('Server not listening')
                //        Reset()

                // If there was no timeout, delay a bit. TODO
                //read.Sleep(10);
                if (!timedOut) Thread.Sleep(50);
            }

            // All done.
            //ts.Dispose();
            //taskKeyboard.Dispose();
            //taskRead.Dispose();
        }
        #endregion

        /// <summary>
        /// For timing measurements.
        /// </summary>
        /// <param name="init">Makes all subsequent calls relative.</param>
        /// <returns></returns>
        double GetCurrentMsec(bool init = false)
        {
            if (init)
            {
                _startMsec = Stopwatch.GetTimestamp();
                return 0;
            }
            else
            {
                return (double)(1000.0 * Stopwatch.GetTimestamp() / Stopwatch.Frequency);
            }
        }

        void Reset()
        {
            _comm?.Reset();

            // Reset watchdog.
            //sendts = 0;

            // # Clear queue.
            _qCli.Clear();
        }


        //// https://stackoverflow.com/questions/22664392/await-console-readline
        //async Task<CliInput> GetInputAsync() //CancellationToken token
        //{
        //    // Wouldn't that be return await Task.Run(() => Console.ReadLine());
        //    return Task.Run(() => 
        //    {
        //        // Check for something to do.
        //        if (Console.KeyAvailable)
        //        {
        //            var key = Console.ReadKey(false);

        //            // Check hot key.
        //            if (MatchMods(_settings.HotKeyMod, key.Modifiers))
        //            {
        //                return new CliInput(_hotKeys[(char)key.Key], key.Modifiers);
        //            }
        //            else
        //            {
        //                // Get the rest - blocks.
        //                var rest = Console.ReadLine();
        //                return new CliInput((char)key.Key + rest, key.Modifiers);
        //            }
        //        }
        //    })
        //}


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
                    var key = Console.ReadKey(false);

                    // Check hot key.
                    if (MatchMods(_settings.HotKeyMod, key.Modifiers))
                    {
                        _qCli.Enqueue(new(_hotKeys[(char)key.Key], key.Modifiers));
                    }
                    else
                    {
                        // Get the rest - blocks.
                        var rest = Console.ReadLine();
                        _qCli.Enqueue(new((char)key.Key + rest, key.Modifiers));
                    }
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        ///// <summary>
        ///// Service the comm read.
        ///// </summary>
        ///// <param name="token"></param>
        //void DoRead(CancellationToken token)
        //{
        //    while (!token.IsCancellationRequested)
        //    {
        //       var (stat, msg, data) = _comm.Receive();

        //       switch (stat)
        //       {
        //           case OpStatus.Success:
        //                _qCommRead.Enqueue(data);
        //               break;

        //           case OpStatus.Error:
        //               _logger.Error($"Comm error: {msg}");
        //               break;

        //           default: // Timeout is ok
        //               break;
        //       }

        //       // Don't be greedy.
        //       Thread.Sleep(20);
        //    }
        //}

        #region Output text
        /// <summary>
        /// Write to console.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="nl">Default is to add a nl.</param>
        void Write(string text, bool nl = true)
        {
            bool hasMatch = false;
            foreach (Matcher m in _config.Matchers)
            {
                if (text.Contains(m.Text))
                {
                    if (m.ForeColor is not ConsoleColorEx.None) { Console.ForegroundColor = (ConsoleColor)m.ForeColor; }
                    if (m.BackColor is not ConsoleColorEx.None) { Console.BackgroundColor = (ConsoleColor)m.BackColor; }
                    Console.Write(m.Text);
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
        #endregion

        #region Settings
        /// <summary>
        /// Load persisted settings.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        void InitFromSettings()
        {
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
                    // do something...
                    //msg = $"Invalid comm args - {e.Message}";
                    //stat = OpStatus.Error;
                    //throw new ArgumentException(msg);
                }

                // Init and check stat.
                //var (stat, msg) = _comm.Init(_config); // do something...

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
            }
        }
        #endregion

        #region Private stuff
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

        void Help()
        {
            var cc = _config is not null ? $"{_config.Name}({_config.CommType})" : "None";
            Write($"current config: {cc}");

            _hotKeys.ForEach(x => Write($"alt-{x.Key} sends: [{x.Value}]"));

            Write("serial ports:");
            var sports = SerialPort.GetPortNames();
            sports.ForEach(s => { Write($"   {s}"); });
        }
        #endregion
    }
}
