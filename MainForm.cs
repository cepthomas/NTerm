using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;


namespace NTerm
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>My logger</summary>
        readonly Logger _logger = LogManager.CreateLogger("NTerm");

        /// <summary>Settings</summary>
        readonly UserSettings _settings = new();

        /// <summary>Current config</summary>
        Config? _config = null;

        /// <summary>Client flavor.</summary>
        IComm? _comm = null;

        /// <summary>User hot keys.</summary>
        readonly Dictionary<string, string> _hotKeys = [];

        /// <summary>Colors - ansi.</summary>
        readonly Dictionary<LogLevel, int> _logColors = new() { { LogLevel.Error, 91 }, { LogLevel.Warn, 93 }, { LogLevel.Info, 96 }, { LogLevel.Debug, 92 } };

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<CliInputEventArgs> _queue = new();

        /// <summary>Queue management.</summary>
        bool _running = false;

        /// <summary>Queue management.</summary>
        readonly CancellationTokenSource _tokenSource = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// 
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Set up log first.
            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += LogMessage;

            //var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            StartPosition = FormStartPosition.Manual;
            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);

            KeyPreview = true;

            //private void Clear_Click(object sender, EventArgs e)
            //private void Wrap_Click(object sender, EventArgs e)

            // Init configuration.
            InitFromSettings();

            cliIn.InputEvent += (object? sender, CliInputEventArgs e) =>
            {
                if (_running) // don't fill a dead queue.
                {
                    _queue.Enqueue(e);
                }
            };
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            _comm?.Dispose();
            _comm = null;

            base.Dispose(disposing);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            Run();

            base.OnLoad(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings();

            base.OnFormClosing(e);
        }
        #endregion



        /// <summary>
        /// Do some global key handling.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // TODO send everything to cli control.

            switch (e.KeyCode)
            {
                case Keys.Space:
                    // Toggle.
                    //   UpdateState(btnPlay.Checked ? ExplorerState.Stop : ExplorerState.Play);
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Main loop.
        /// </summary>
        public void Run()
        {
            _running = true;

            Task task = Task.Run(async () =>
            {
                while (_running)
                {
                    if (_tokenSource.Token.IsCancellationRequested)
                    {
                        _running = false;
                    }

                    try
                    {
                        while (_queue.TryDequeue(out CliInputEventArgs? le))
                        {
                            // Do the work...

                            //bool running = true;
                            string? ucmd = null;
                            OpStatus res;
                            bool ok = true;

                            switch (le.Mod, le.Text)
                            {
                                case (Modifier.None, _):
                                    // Normal line, send it.
                                    _logger.Trace($">>> got line:{le.Text}");
                                    ucmd = le.Text;
                                    break;

                                //case (Modifier.Ctrl, "q"):
                                //    _tokenSource.Cancel();
                                //    break;

                                //case (Modifier.Ctrl, "s"):
                                //    var ed = new Editor() { Settings = _settings };
                                //    ed.ShowDialog();
                                //    LoadSettings();
                                //    break;

                                //case (Modifier.Ctrl, "h"):
                                //    Help();
                                //    break;

                                case (Modifier.Alt, _):
                                    ok = _hotKeys.TryGetValue(le.Text, out ucmd);
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
                                    _logger.Debug($"SND:{ucmd}");
                                    res = _comm.Send(ucmd);
                                    // Show results.
                                    _logger.Debug($"RCV:{res}: {_comm.Response}");
                                }
                                // WritePrompt();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Do something or just leave it alone?
                        throw;
                    }

                    // Rest a bit.
                    await Task.Delay(10);
                }
            }, _tokenSource.Token);
        }


        #region Settings
        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        void InitFromSettings()
        {
            // Reset.
            _config = null;
            _comm?.Dispose();
            _comm = null;

            cliIn.BackColor = _settings.BackColor;
            cliIn.Font = _settings.Font;
            tvOut.BackColor = _settings.BackColor;
            tvOut.Font = _settings.Font;

            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;

            var lc = _settings.Configs.Select(x => x.Name).ToList();

            if (lc.Count > 0)
            {
                var c = _settings.Configs.FirstOrDefault(x => x.Name == _settings.CurrentConfig);
                _config = c is null ? _settings.Configs[0] : c;

                OpStatus stat = OpStatus.Success;

                _comm = _config.CommType switch
                {
                    CommType.Tcp => new TcpComm(),
                    CommType.Serial => new SerialComm(new SerialPortImpl()),
                    CommType.Null => new NullComm(),
                    _ => throw new NotImplementedException(),
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
            if (_settings.AnsiColor)
            {
                tvOut.AppendAnsi(s);
            }
            else
            {
                tvOut.Append(s);
            }
        }
        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Click(object sender, EventArgs e)
        {
            var ed = new Editor() { Settings = _settings };
            ed.ShowDialog();
            InitFromSettings();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Help_Click(object sender, EventArgs e)
        {
            Write("ctrl-s to edit settings");
            Write("ctrl-q to exit");
            Write("ctrl-h this here");

            _hotKeys.ForEach(x => Write($"alt-{x.Key} sends: [{x.Value}]"));

            var cc = _config is not null ? $"{_config.Name}({_config.CommType})" : "None";
            Write($"current config: {cc}");

            Write("serial ports:");
            SerialPort.GetPortNames().ForEach(s => { Write($"   {s}"); });
        }

    }
}
