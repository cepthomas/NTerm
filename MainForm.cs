using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
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
        #region Types
        /// <summary></summary>
        enum ColorMode { None, Ansi, Match }
        
        /// <summary></summary>
        enum Modifier { None, Ctrl, Alt }

        /// <summary>Spec for one match.</summary>
        /// <param name="Text"></param>
        /// <param name="WholeWord"></param>
        /// <param name="WholeLine"></param>
        /// <param name="ForeColor"></param>
        /// <param name="BackColor"></param>
        record Matcher(string Text, bool WholeWord, bool WholeLine, Color? ForeColor, Color? BackColor);
        #endregion

        #region Fields
        /// <summary>My logger</summary>
        readonly Logger _logger = LogManager.CreateLogger("NTerm");

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

        /// <summary>Limit the output size. TODO use lines</summary>
        int _maxText = 10000;

        /// <summary>Internal data container.</summary>
        record CliInput(Modifier Mod, string Text);

        /// <summary>Cli async event queue.</summary>
        readonly ConcurrentQueue<CliInput> _queue = new();

        // [DisplayName("Color Mode TODO or in Config? + matchers?")]
        // [Description("Colorize Mode.")]
        // [Browsable(true)]
        // public ColorMode ColorMode { get; set; } = ColorMode.None;
        ColorMode _colorMode = ColorMode.Ansi;

        /// <summary>All the match specs.</summary>
        List<Matcher> _matchers = [];

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
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => Write(e.Message);

            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            StartPosition = FormStartPosition.Manual;
            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            KeyPreview = true;

            // Init from previously loaded settings.
            InitFromSettings();

            // TODO button images?
            //btnSettings.DisplayStyle = ToolStripItemDisplayStyle.Text;
            //btnSettings.Image = Image.FromFile("C:\\Dev\\Apps\\NTerm\\Ramon.png");

            // UI handlers.
            rtbIn.KeyDown += RtbIn_KeyDown;
            rtbOut.KeyDown += RtbOut_KeyDown;
            btnSettings.Click += (_, _) => SettingsEditor.Edit(_settings, "User Settings", 500);
            btnClear.Click += (_, _) => rtbOut.Clear();
            btnWrap.Click += (_, _) => rtbOut.WordWrap = btnWrap.Checked;

            _ansiForeColor = rtbOut.ForeColor;
            _ansiBackColor = rtbOut.BackColor;


            btnDebug.Click += (_, _) => TestRegex();
        }

        void TestRegex() // TODO put in Test.
        {
            // https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.group.captures

            string s = "No color [31m Standard color [38;5;32m  256 Color [38;2;60;120;180m  RGB Color [0m reset";

            /* >>>
            Matched:0-14
                Group0:No color [31m
                  Capture0:No color [31m
                Group1:No color 
                  Capture0:No color 
                Group2:31
                  Capture0:31
            Matched:14-27
                Group0: Standard color [38;5;32m
                  Capture0: Standard color [38;5;32m
                Group1: Standard color
                  Capture0: Standard color
                Group2:38;5;32
                  Capture0:38;5;32
            Matched:41-31
                Group0:  256 Color [38;2;60;120;180m
                  Capture0:  256 Color [38;2;60;120;180m
                Group1:  256 Color 
                  Capture0:  256 Color
                Group2:38;2;60;120;180
                  Capture0:38;2;60;120;180
            Matched:72-17
                Group0:  RGB Color [0m
                  Capture0:  RGB Color [0m
                Group1:  RGB Color
                  Capture0:  RGB Color
                Group2:0
                  Capture0:0
            Dangling: reset
            */

            int end = 0;
            var matches = Regex.Matches(s, _ansiPattern);
            foreach (Match match in Regex.Matches(s, _ansiPattern))
            {
                end = match.Index + match.Length;

                //Write($"Matched text: {match.Value} [{match.Index} {match.Length}]");
                Write($"Matched:{match.Index}-{match.Length}");

                int groupCtr = 0;
                foreach (Group group in match.Groups)
                {
                    Write($"  Group{groupCtr}:{group.Value}");
                    groupCtr++;

                    int capCtr = 0;
                    foreach (Capture capture in group.Captures)
                    {
                        Write($"    Capture{capCtr}:{capture.Value}");
                        capCtr++;
                    }
                }
            }

            var dangling = s.Substring(end);
            Write($"Dangling:{dangling}");
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

        #region Main loop
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
                    _running = !_tokenSource.Token.IsCancellationRequested;

                    try
                    {
                        while (_queue.TryDequeue(out CliInput? le))
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
                                    Write(_comm.Response);
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
        #endregion

        #region Key handlers
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RtbOut_KeyDown(object? sender, KeyEventArgs e)// TODO
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RtbIn_KeyDown(object? sender, KeyEventArgs e)// TODO
        {
            //var cv = (char)e.KeyValue;
            //var iv = e.KeyValue;
            //char creal = e.KeyValue >= 65 && e.KeyValue <= 90 && e.Shift ? (char)e.KeyValue : (char)(e.KeyValue + 32);
            ////char creal = (char)e.KeyValue;
            //char ctest = char.ToLower(creal);
            //Debug.WriteLine($">>> RTB:{creal} {ctest}");
            ////Debug.WriteLine($">>> RTB:{e.KeyCode} {e.KeyValue}");

            //if (sender == null)
            //{
            //    rtbIn.Text += creal;
            //}
            //char creal = e.KeyValue >= 65 && e.KeyValue <= 90 && e.Shift ? (char)e.KeyValue : (char)(e.KeyValue + 32);

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
                        //InputEvent?.Invoke(this, la);
                        // Clear line.
                        rtbIn.Text = $"{_settings.Prompt}";
                    }
                    break;

                case (false, false, Keys.Escape):
                    // Throw away current.
                    rtbIn.Text = $"{_settings.Prompt}";
                    break;

                case (false, false, Keys.Up):
                    // Go through history older.
                    if (_historyIndex < _history.Count - 1)
                    {
                        _historyIndex++;
                        rtbIn.Text = $"{_settings.Prompt}{_history[_historyIndex]}";
                    }
                    break;

                case (false, false, Keys.Down):
                    // Go through history newer.
                    if (_historyIndex > 0)
                    {
                        _historyIndex--;
                        rtbIn.Text = $"{_settings.Prompt}{_history[_historyIndex]}";
                    }
                    break;

                case (true, false, _) when IsAlNum(e.KeyCode):
                    // Hot key?
                    _queue.Enqueue(new(Modifier.Ctrl, e.KeyCode.ToString()));
                    //InputEvent?.Invoke(this, new() { Mod = ModifierXXX.Ctrl, Text = e.KeyCode.ToString() });
                    break;

                case (false, true, _) when IsAlNum(e.KeyCode):
                    // Hot key?
                    _queue.Enqueue(new(Modifier.Alt, e.KeyCode.ToString()));
                    //InputEvent?.Invoke(this, new() { Mod = ModifierXXX.Alt, Text = e.KeyCode.ToString() });
                    break;
            }

            bool IsAlNum(Keys key)
            {
                return (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z);
            }
        }

        /// <summary>
        /// Send all keystrokes to the cli.// TODO
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Send everything to cli control.
            //cliIn.ProcessKey(e);
            //https://stackoverflow.com/questions/1264227/send-keystroke-to-other-control
            //SendKeys.SendWait();

            Debug.WriteLine($">>> MAI:{e.KeyCode}");

            //e.Handled = true;

            base.OnKeyDown(e);
        }
        #endregion

        #region Output text
        /// <summary>
        /// Top level outputter. Takes care of UI thread.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="nl"></param>
        void Write(string text, bool nl = true)
        {
            this.InvokeIfRequired(_ =>
            {
                // Trim buffer.
                if (rtbOut.TextLength > _maxText)
                {
                    int end = _maxText / 5;
                    while (rtbOut.Text[end] != (char)Keys.LineFeed) end++;
                    rtbOut.Select(0, end);
                    rtbOut.SelectedText = "";
                }

                bool ok = _colorMode switch
                {
                    ColorMode.None => WritePlain(text),
                    ColorMode.Ansi => WriteAnsi(text),
                    ColorMode.Match => WriteMatch(text),
                    _ => false
                };

                rtbOut.ScrollToCaret();

                if (nl)
                {
                    rtbOut.AppendText(Environment.NewLine);
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        bool WritePlain(string text)
        {
            rtbOut.AppendText(text);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        bool WriteAnsi(string text)
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
        bool WriteMatch(string text)
        {
            // This could use some clumsy regex. Eric Lippert says don't bother: https://stackoverflow.com/q/48294100.

            foreach (Matcher m in _matchers)
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
        /// Persist.
        /// </summary>
        public void SaveSettings()
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
                    (0, 0, 0), (127, 0, 0), (0, 127, 0), (127, 127, 0), (0, 0, 127), (127, 0, 127), (0, 127, 127), (191, 191, 191),
                    (127, 127, 127), (0, 0, 0), (0, 255, 0), (255, 255, 0), (0, 0, 255), (255, 0, 255), (0, 255, 255), (255, 255, 255)];
                return Color.FromArgb(std_colors[id].r, std_colors[id].g, std_colors[id].b);
            }
        }

        // /// <summary>
        // /// 
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // void LogMessage(object? sender, LogMessageEventArgs e)
        // {
        //     _logColors.TryGetValue(e.Level, out int color);
        //     Write($"\u001b[{color}m{e.Message}\u001b[0m");
        // }
        // /// <summary>Colors - ansi. TODO use ??</summary>
        // readonly Dictionary<LogLevel, int> _logColors = new() { { LogLevel.Error, 91 }, { LogLevel.Warn, 93 }, { LogLevel.Info, 96 }, { LogLevel.Debug, 92 } };

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

            _hotKeys.ForEach(x => Write($"alt-{x.Key} sends: [{x.Value}]"));

            var cc = _config is not null ? $"{_config.Name}({_config.CommType})" : "None";
            Write($"current config: {cc}");

            Write("serial ports:");
            var sports = SerialPort.GetPortNames();
            sports.ForEach(s => { Write($"   {s}"); });
        }
        #endregion
    }
}
