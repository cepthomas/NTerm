using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>My logger</summary>
        readonly Logger _logger = LogManager.CreateLogger("MAI");

        /// <summary>Settings</summary>
        readonly UserSettings _settings = new();

        /// <summary>Current config</summary>
        Config? _config = null;

        /// <summary>Client comm flavor.</summary>
        IComm? _comm = null;

        /// <summary>Queue management.</summary>
        bool _running = false;

        /// <summary>Queue management.</summary>
        readonly CancellationTokenSource _tokenSource = new();

        /// <summary>User hot keys.</summary>
        readonly Dictionary<string, string> _hotKeys = [];

        /// <summary>Most recent at beginning.</summary>
        List<string> _history = [];

        /// <summary>Current location in list.</summary>
        int _historyIndex = 0;

        /// <summary>Limit the output size.</summary>
        int _maxLines = 200;

        /// <summary>Cli async event queue.</summary>
        readonly ConcurrentQueue<CliInput> _queue = new();

        /// <summary>Ansi regex.</summary>
        string _ansiPattern = @"([^\u001B]+)\u001B\[([^m)]+)m";

        /// <summary>Current ansi color.</summary>
        Color _ansiForeColor;

        /// <summary>Current ansi color.</summary>
        Color _ansiBackColor;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Build me one.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            // Set up log first.
            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => Print(e.ShortMessage);

            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            StartPosition = FormStartPosition.Manual;
            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Route all app key strokes through this form first.
            //KeyPreview = true;
            // Force all keystrokes to the cli input.
            //rtbOut.Enabled = false;

            // Init from previously loaded settings.
            InitFromSettings();

            // TODO button images?
            //btnSettings.DisplayStyle = ToolStripItemDisplayStyle.Text;
            //btnSettings.Image = Image.FromFile("C:\\Dev\\Apps\\NTerm\\Ramon.png");

            // UI handlers.
            rtbIn.KeyDown += RtbIn_KeyDown;
            //rtbOut.KeyDown += RtbOut_KeyDown;
            //rtbIn.KeyPress += (object? sender, KeyPressEventArgs e) => Debug.WriteLine($"KeyPress:{e.KeyChar}");
            //rtbOut.KeyDown += (object? sender, KeyEventArgs e) => throw new NotImplementedException();
            btnSettings.Click += (_, _) => SettingsEditor.Edit(_settings, "User Settings", 500);
            btnClear.Click += (_, _) => rtbOut.Clear();
            btnWrap.Click += (_, _) => rtbOut.WordWrap = btnWrap.Checked;
            //btnDebug.Click += (_, _) => rtbOut.BackColor = Color.Pink;

            _ansiForeColor = rtbOut.ForeColor;
            _ansiBackColor = rtbOut.BackColor;

            //_logger.Trace($"=== Trace ===");
            //_logger.Debug($"=== Debug ===");
            //_logger.Info($"=== Info ===");
            //_logger.Error($"=== Error ===");
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
        /// Time to run the application.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Run();
        }

        /// <summary>
        /// Shut her down.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _tokenSource.Cancel();
            SaveSettings();
            base.OnFormClosing(e);
        }
        #endregion

        #region Main loop
        /// <summary>
        /// Main loop.
        /// </summary>
        void Run()
        {
            _running = true;

            if (_config is null)
            {
                _logger.Warn($"Client is not selected - edit your settings");
                return;
            }

            if (_comm is null)
            {
                _logger.Warn($"Comm is not initialized - edit your settings");
                return;
            }

            Task task = Task.Run(async () =>
            {
                while (_running)
                {
                    _running = !_tokenSource.Token.IsCancellationRequested;

                    try
                    {
                        if (_queue.TryDequeue(out CliInput? le))
                        {
                            // Got one - do the work...
                            string? ucmd = null;

                            switch (le.Mod, le.Text)
                            {
                                case (Modifier.None, _):
                                    // Normal line, send it.
                                    //_logger.Trace($">>> got line:{le.Text}");
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
                                    _hotKeys.TryGetValue(le.Text, out ucmd);
                                    break;

                                default:
                                    Print("Invalid command");
                                    break;
                            }

                            if (ucmd is not null)
                            {
                                _logger.Debug($"SND:{ucmd}");
                                var (stat, resp) = _comm.Send(ucmd);
                                _logger.Debug($"RCV [{stat}]:{resp}");

                                switch (stat)
                                {
                                    case OpStatus.Success:
                                        Print(resp, false);
                                        break;

                                    case OpStatus.Timeout:
                                        // Nothing to do.
                                        break;

                                    case OpStatus.NoResp:
                                        // Nothing to do.
                                        break;

                                    case OpStatus.Error:
                                        Print(resp);
                                        break;
                                }

                                // WritePrompt();
                            }
                        }

                        // Maybe poll.
                        if (_config.CommMode == CommMode.Poll)
                        {
                            var (stat, resp) = _comm.Send(null);

                            switch (stat)
                            {
                                case OpStatus.Success:
                                    Print(resp, false);
                                    break;

                                case OpStatus.Error:
                                    Print(resp);
                                    break;

                                case OpStatus.Timeout:
                                case OpStatus.NoResp:
                                    // Nothing to do.
                                    break;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // TODO Do something or just leave it alone?
                        throw;
                    }

                    // Rest a bit.
                    await Task.Delay(10);
                }
            }, _tokenSource.Token);
        }
        #endregion

        #region Key handlers
        // /// <summary>
        // /// Top level handler via KeyPreview. Send all keystrokes to the cli.  TODO
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // protected override void OnKeyDown(KeyEventArgs e)
        // {   
        //     Write($">>> OnKeyDown:[{e.KeyCode}]");
        //     // Route everything to cli control.
        //     //ProcessKey(e);
        //     //e.Handled = true;
        //     base.OnKeyDown(e);
        // }

        /// <summary>
        /// User wants to do something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RtbIn_KeyDown(object? sender, KeyEventArgs e)
        {
            // PrintLine($">>> RtbIn_KeyDown:[{e.KeyCode}]");
            ProcessKey(e);
            e.Handled = true;
        }

        // void RtbOut_KeyDown(object? sender, KeyEventArgs e)
        // {
        //     PrintLine($">>> RtbOut_KeyDown:[{e.KeyCode}]");
        //     //ProcessKey(e);
        //     //e.Handled = true;
        // }

        /// <summary>
        /// User wants to do something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ProcessKey(KeyEventArgs e)
        {
            // Check for text input.
            var ch = KeyUtils.KeyToChar(e.KeyCode, e.Modifiers).ch;
            // if (ch > 0) { Debug.WriteLine($"char:{ch}"); }
            //ProcessKey(ActiveControl, e);

            switch (e.Control, e.Alt, e.KeyCode)
            {
                case (false, false, Keys.Enter):
                    if (rtbIn.Text.Length > 0)
                    {
                        // Complete line. Add to history and put in queue.
                        var t = rtbIn.Text;
                        AddToHistory(t);

                        CliInput la = new(Modifier.None, t);
                        _queue.Enqueue(la);
                        // Clear line.
                        rtbIn.Text = "";// $"{_settings.Prompt}";
                    }
                    break;

                case (false, false, Keys.Escape):
                    // Throw away current.
                    rtbIn.Text = "";// $"{_settings.Prompt}";
                    break;

                case (false, false, Keys.Up):
                    // Go through history older.
                    if (_historyIndex < _history.Count - 1)
                    {
                        _historyIndex++;
                        //rtbIn.Text = $"{_settings.Prompt}{_history[_historyIndex]}";
                        rtbIn.Text = _history[_historyIndex];
                    }
                    break;

                case (false, false, Keys.Down):
                    // Go through history newer.
                    if (_historyIndex > 0)
                    {
                        _historyIndex--;
                        //rtbIn.Text = $"{_settings.Prompt}{_history[_historyIndex]}";
                        rtbIn.Text = _history[_historyIndex];
                    }
                    break;

                case (true, false, _) when ch > 0:
                    // Hot key?
                    _queue.Enqueue(new(Modifier.Ctrl, ch.ToString()));
                    break;

                case (false, true, _) when ch > 0:
                    // Hot key?
                    _queue.Enqueue(new(Modifier.Alt, ch.ToString()));
                    break;
            }
        }
        #endregion

        #region Output text
        /// <summary>
        /// Top level outputter. Takes care of UI thread.
        /// </summary>
        /// <param name="text">What to print</param>
        /// <param name="nl">Default is to add a nl. Text from server is expected to supply them instead.</param>
        void Print(string text, bool nl = true)
        {
            this.InvokeIfRequired(_ =>
            {
                // Trim buffer.
                int numLines = rtbOut.GetLineFromCharIndex(rtbOut.TextLength - 1);
                //int overflowLines = rtbOut.GetLineFromCharIndex(rtbOut.TextLength - 1) - _maxLines;
                if (numLines - _maxLines > 5)
                {
                    rtbOut.Select(0, rtbOut.GetFirstCharIndexFromLine(6));
                    rtbOut.SelectedText = "";
                }

                // Harmonize nx line endings.
                text = text.Replace("\n", "\r\n").Replace("\r\r", "");

                // Do it.
                bool ok = _settings.ColorMode switch
                {
                    ColorMode.None => PrintPlain(text),
                    ColorMode.Ansi => PrintAnsi(text),
                    ColorMode.Match => PrintMatch(text),
                    _ => false
                };

                if (nl)
                {
                    rtbOut.AppendText(Environment.NewLine);
                }

                rtbOut.ScrollToCaret();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        bool PrintPlain(string text)
        {
            rtbOut.AppendText(text);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        bool PrintAnsi(string text)
        {
            int end = 0;

            var matches = Regex.Matches(text, _ansiPattern);

            foreach (Match match in matches)
            {
                end = match.Index + match.Length;

                // Write text in group.
                rtbOut.AppendText(match.Groups[1].Value);

                // Update colors.
                var clrs = ColorFromAnsi(match.Groups[2].Value);
                _ansiBackColor = clrs.bg;
                _ansiForeColor = clrs.fg;
            }

            var trailing = text.Substring(end);
            rtbOut.AppendText(trailing);

            return true;
        }

        /// <summary>
        /// Use simple matching.
        /// </summary>
        /// <param name="text"></param>
        bool PrintMatch(string text)
        {
            // This could use some clumsy regex. Eric Lippert says don't bother: https://stackoverflow.com/q/48294100.

            foreach (Matcher m in _settings.Matchers)
            {
                int pos = 0;

                do
                {
                    pos = text.IndexOf(m.Text, pos);

                    if (pos >= 0)
                    {
                        if (m.WholeWord)
                        {
                            // Check neighbors.
                            bool leftww = pos == 0 || !char.IsAsciiLetterOrDigit(text[pos - 1]);
                            bool rightww = pos + m.Text.Length >= Text.Length || !char.IsAsciiLetterOrDigit(text[pos + 1]);

                            if (leftww && rightww)
                            {
                                DoOneMatch(m);
                            }
                            else
                            {
                                rtbOut.AppendText(m.Text);
                            }
                        }
                        else
                        {
                            DoOneMatch(m);
                        }
                    }
                }
                while (pos >= 0);
            }

            // Local function.
            void DoOneMatch(Matcher m)
            {
                // cache
                var fc = rtbOut.SelectionColor;
                var bc = rtbOut.SelectionBackColor;
                rtbOut.SelectionColor = m.ForeColor ?? rtbOut.ForeColor;
                rtbOut.SelectionBackColor = m.BackColor ?? rtbOut.BackColor;
                rtbOut.AppendText(m.Text);
                // restore
                rtbOut.SelectionColor = fc;
                rtbOut.SelectionBackColor = bc;
            }

            return true;
        }
        #endregion

        #region Settings
        /// <summary>
        /// Handle persisted settings.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        void InitFromSettings()
        {
            // Reset.
            _config = null;
            _comm?.Dispose();
            _comm = null;

            rtbIn.BackColor = _settings.BackColor;
            rtbIn.Font = _settings.Font;
            rtbOut.BackColor = _settings.BackColor;
            rtbOut.Font = _settings.Font;

            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;

            var lc = _settings.Configs.Select(x => x.Name).ToList();

            if (lc.Count > 0)
            {
                var c = _settings.Configs.FirstOrDefault(x => x.Name == _settings.CurrentConfig);
                _config = c is null ? _settings.Configs[0] : c;

                _comm = _config.CommType switch
                {
                    CommType.Tcp => new TcpComm(),
                    CommType.Serial => new SerialComm(),
                    CommType.None => new NullComm(),
                    _ => throw new NotImplementedException(),
                };

                // Init and check stat.
                var r = _comm.Init(_config);

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

                _logger.Info($"Using {_config.Name} ({_config.CommType})");
                return;
            }

            _comm?.Dispose();
            _comm = null;
            _config = null;
            _logger.Warn($"Comm is not initialized - edit settings");
        }

        /// <summary>
        /// Persist.
        /// </summary>
        void SaveSettings()
        {
            _settings.FormGeometry = new Rectangle(Location.X, Location.Y, Size.Width, Size.Height);
            _settings.Save();
        }
        #endregion

        #region Misc
        /// <summary>
        /// Decode ansi escape sequence arguments.
        /// </summary>
        /// <param name="ansi">Ansi args string</param>
        /// <returns>Foreground and background colors. Color is Empty if invalid ansi string.</returns>
        (Color fg, Color bg) ColorFromAnsi(string ansi)
        {
            Color fg = Color.Empty;
            Color bg = Color.Empty;

            var parts = ansi.SplitByToken(";").Select(i => int.Parse(i)).ToList();

            var p0 = parts.Count >= 1 ? parts[0] : 0;
            var p1 = parts.Count >= 2 ? parts[1] : 0;
            var p2 = parts.Count == 3 ? parts[2] : 0;

            switch (parts.Count)
            {
                /////// Standard 8/16 colors. ESC[IDm
                case 1 when p0 >= 30 && p0 <= 37:
                    fg = MakeStdColor(p0 - 30);
                    break;

                case 1 when p0 >= 40 && p0 <= 47:
                    bg = MakeStdColor(p0 - 40);
                    //invert = true;
                    break;

                case 1 when p0 >= 90 && p0 <= 97:
                    fg = MakeStdColor(p0 - 90);
                    break;

                case 1 when p0 >= 100 && p0 <= 107:
                    bg = MakeStdColor(p0 - 100);
                    //invert = true;
                    break;

                /////// 256 colors. ESC[38;5;IDm  ESC[48;5;IDm
                case 3 when (p0 == 38 || p0 == 48) && p1 == 5 && p2 >= 0 && p2 <= 15:
                    // 256 colors - standard color.
                    var clr1 = MakeStdColor(p2);
                    if (p0 == 48) bg = clr1; else fg = clr1;
                    break;

                case 3 when (p0 == 38 || p0 == 48) && p1 == 5 && p2 >= 16 && p2 <= 231:
                    // 256 colors - rgb color.
                    int[] map6 = [0, 95, 135, 175, 215, 255];
                    int im = p2 - 16;
                    int r = map6[im / 36];
                    int g = map6[(im / 6) % 6];
                    int b = map6[im % 6];

                    var clr2 = Color.FromArgb(r, g, b);
                    if (p0 == 48) bg = clr2; else fg = clr2;
                    break;

                case 3 when (p0 == 38 || p0 == 48) && p1 == 5 && p2 >= 232 && p2 <= 255:
                    // 256 colors - grey
                    int i = p2 - 232; // 0 - 23
                    int grey = i * 10 + 8;

                    var clr3 = Color.FromArgb(grey, grey, grey);
                    if (p0 == 48) bg = clr3; else fg = clr3;
                    break;

                /////// Explicit rgb colors. ESC[38;2;R;G;Bm  ESC[48;2;R;G;Bm
                case 5 when p0 == 38 || p0 == 48 && p1 == 2:

                    var clr4 = Color.FromArgb(parts[2], parts[3], parts[4]);
                    if (p0 == 48) bg = clr4; else fg = clr4;
                    break;
            }

            return (fg, bg);

            static Color MakeStdColor(int id)
            {
                (int r, int g, int b)[] std_colors = [
                    (0, 0, 0),       (127, 0, 0),   (0, 127, 0),   (127, 127, 0),
                    (0, 0, 127),     (127, 0, 127), (0, 127, 127), (191, 191, 191),
                    (127, 127, 127), (0, 0, 0),     (0, 255, 0),   (255, 255, 0),
                    (0, 0, 255),     (255, 0, 255), (0, 255, 255), (255, 255, 255)];
                return Color.FromArgb(std_colors[id].r, std_colors[id].g, std_colors[id].b);
            }
        }

        /// <summary>
        /// Update the history with the new entry.
        /// </summary>
        /// <param name="s"></param>
        void AddToHistory(string s)
        {
            if (s.Length > 0)
            {
                var newlist = new List<string> { s };
                // Check for dupes and max size.
                _history.ForEach(v => { if (!newlist.Contains(v) && newlist.Count <= 20) newlist.Add(v); });
                _history = newlist;
                _historyIndex = 0;
            }
        }

        /// <summary>
        /// Help.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Help_Click(object sender, EventArgs e)
        {
            // Write("ctrl-s to edit settings");
            // Write("ctrl-q to exit");
            // Write("ctrl-h this here");

            _hotKeys.ForEach(x => Print($"alt-{x.Key} sends: [{x.Value}]"));

            var cc = _config is not null ? $"{_config.Name}({_config.CommType})" : "None";
            Print($"current config: {cc}");

            Print("serial ports:");
            var sports = SerialPort.GetPortNames();
            sports.ForEach(s => { Print($"   {s}"); });
        }
        #endregion
    }
}
