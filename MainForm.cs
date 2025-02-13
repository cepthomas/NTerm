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
        readonly ConcurrentQueue<CliInputEventArgsXXX> _queue = new();

        /// <summary>Queue management.</summary>
        bool _running = false;

        /// <summary>Queue management.</summary>
        readonly CancellationTokenSource _tokenSource = new();
        #endregion


        /// <summary>Limit the size.</summary>
        public int MaxText { get; set; } = 10000;


        /// <summary>For hotkeys.</summary>
        public enum ModifierXXX { None, Ctrl, Alt }

        public class CliInputEventArgsXXX : EventArgs
        {
            /// <summary>Test for hotkey press.</summary>
            public ModifierXXX Mod { get; set; } = ModifierXXX.None;

            /// <summary>User text, no eol.</summary>
            public string Text { get; set; } = "";

            ///// <summary>Client has taken ownership of the data.</summary>
            //public bool Handled { get; set; } = false;
        }




        #region Fields

        ///// <summary>Contained control.</summary>
        //readonly RichTextBox rtbOut;
        enum ParseState
        {
            Idle,       // Plain text
            Look,       // Looking for '[' in sequence
            Collect     // Collect sequence args
        }

        /// <summary>Accumulated ansi arguments.</summary>
        string _ansiArgs = "";

        // Keys.
        const char CANCEL = (char)0x03;
        const char BACKSPACE = (char)0x08;
        const char TAB = (char)0x09; // horizontal tab
        const char LINEFEED = (char)0x0A; // Line feed
        const char CLEAR = (char)0x0C; // Form feed
        const char RETURN = (char)0x0D; // Carriage return
        const char ESCAPE = (char)0x1B;
        #endregion

        /// <summary>Ansi parse state.</summary>
        ParseState _state = ParseState.Idle;


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

            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            StartPosition = FormStartPosition.Manual;
            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            KeyPreview = true;

            // Init configuration.
            InitFromSettings();


            //public Console()
            //{
            //    rtbOut = new()
            //    {
            //        Dock = DockStyle.Fill,
            //        Font = new Font("Cascadia Code", 9),
            //        BorderStyle = BorderStyle.None,
            //        ForeColor = Color.Black,
            //        ReadOnly = true,
            //        ScrollBars = RichTextBoxScrollBars.Both,
            //    };
            //    rtbOut.KeyDown += Rtb_KeyDown;
            //    Controls.Add(rtbOut);
            //}


            //public CliInput()
            //{
            //    rtbIn = new()
            //    {
            //        Dock = DockStyle.Fill,
            //        Font = new Font("Consolas", 10), // TODO from settings
            //        BorderStyle = BorderStyle.None,
            //        ForeColor = Color.Black,
            //        Multiline = false,
            //        ReadOnly = false,
            //        ScrollBars = RichTextBoxScrollBars.Horizontal,
            //        AcceptsTab = true,
            //        TabIndex = 0,
            //        Text = ""
            //    };
            //   // rtbIn.KeyDown += Rtb_KeyDown;
            //    Controls.Add(rtbIn);
            //}

            rtbIn.KeyDown += RtbIn_KeyDown;
            rtbOut.KeyDown += RtbOut_KeyDown;


            // TODO buttons?
            //btnSettings.DisplayStyle = ToolStripItemDisplayStyle.Text;
            //btnSettings.Image = Image.FromFile("C:\\Dev\\Apps\\NTerm\\Ramon.png");

            // UI handlers.
            btnSettings.Click += (_, _) => SettingsEditor.Edit(_settings, "User Settings", 500);
            btnClear.Click += (_, _) => rtbOut.Clear();
            btnWrap.Click += (_, _) => rtbOut.WordWrap = btnWrap.Checked;
//            cliIn.InputEvent += (object? sender, CliInputEventArgsXXX e) => _queue.Enqueue(e);
        }

        private void RtbOut_KeyDown(object? sender, KeyEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RtbIn_KeyDown(object? sender, KeyEventArgs e)
        {
            //65   41   01000001 &#65;     A       UC A
            //90   5A   01011010 &#90;     Z       UC Z

            //97   61   01100001 &#97;     a       LC a
            //122  7A   01111010 &#122;    z       LC z

            // Keys:
            // D0 = 0x30, // 0
            // D9 = 0x39, // 9
            // A = 0x41,
            // Z = 0x5A,

            //public Keys KeyCode - key code for the event.
            //public int KeyValue - integer representation of the KeyCode property. => (int)(KeyData & Keys.KeyCode);
            //public Keys KeyData - key code for the key that was pressed, combined with modifier flags
            // Alt, Control, Shift

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
                        // Add to history and notify client.
                        var t = rtbIn.Text;
                        AddToHistory(t);

//                        CliInputEventArgsXXX la = new() { Text = t };
//                        InputEvent?.Invoke(this, la);
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
//                    InputEvent?.Invoke(this, new() { Mod = ModifierXXX.Ctrl, Text = e.KeyCode.ToString() });
                    break;

                case (false, true, _) when IsAlNum(e.KeyCode):
                    // Hot key?
//                    InputEvent?.Invoke(this, new() { Mod = ModifierXXX.Alt, Text = e.KeyCode.ToString() });
                    break;
            }

            bool IsAlNum(Keys key)
            {
                return (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) || (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z);
            }
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



        /// <summary>Most recent at beginning.</summary>
        List<string> _history = [];

        /// <summary>Current location in list.</summary>
        int _historyIndex = 0;



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

        //public void AppendColor(string text, Color? fg = null, Color? bg = null, bool nl = true)
        //{
        //    this.InvokeIfRequired(_ =>
        //    {
        //        rtbOut.SelectionColor = (Color)(fg == null ? rtbOut.ForeColor : fg);
        //        rtbOut.SelectionBackColor = (Color)(bg == null ? rtbOut.SelectionBackColor : bg);

        //        Write(text, nl);
        //    });
        //}




        /// <summary>
        /// Output text wwith ansi encoding.    
        /// </summary>
        /// <param name="text">The text to show.</param>
        /// <param name="nl">Add new line.</param>
        public void AppendAnsi(string text, bool nl = true)
        {
            this.InvokeIfRequired(_ =>
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];

                    switch (_state, c)
                    {
                        case (ParseState.Idle, ESCAPE):
                            _state = ParseState.Look;
                            break;

                        case (ParseState.Idle, RETURN):
                            _ansiArgs = "";
                            Write("");
                            break;

                        case (ParseState.Look, '['):
                            _ansiArgs = "";
                            _state = ParseState.Collect;
                            break;

                        case (ParseState.Collect, 'm'):
                            var (fg, bg) = ColorFromAnsi(_ansiArgs);
                            rtbOut.SelectionColor = fg;
                            rtbOut.SelectionBackColor = bg;
                            _state = ParseState.Idle;
                            _ansiArgs = "";
                            break;

                        case (ParseState.Collect, _):
                            _ansiArgs += c;
                            break;

                        // TODO these useful?
                        //case (ParseState.Idle, CANCEL):
                        //case (ParseState.Idle, BACKSPACE):
                        //case (ParseState.Idle, TAB):
                        //case (ParseState.Idle, LINEFEED):
                        //case (ParseState.Idle, CLEAR):
                        //    break;

                        case (ParseState.Idle, _):
                            Write(c.ToString(), false);
                            break;

                        case (_, _):
                            // Anything else is a syntax error.
                            Write($"ERROR<{_ansiArgs}", false);
                            _state = ParseState.Idle;
                            break;
                    }
                }

                Write("", nl);
            });
        }

        /// <summary>
        /// Output text using text matching color.
        /// </summary>
        /// <param name="text">The text to show.</param>
        /// <param name="line">Light up whole line otherwise just the word.</param>
        /// <param name="nl">Add new line.</param>
        //public void AppendMatch(string text, bool line = true, bool nl = true)
        //{
        //    this.InvokeIfRequired(_ =>
        //    {
        //        //TODO use regex?
        //        if (line)
        //        {
        //            foreach (string s in MatchText.Keys)
        //            {
        //                if (text.Contains(s))
        //                {
        //                    rtbOut.SelectionBackColor = MatchText[s];
        //                    break;
        //                }
        //            }

        //            Write(text, nl);
        //        }
        //        else
        //        {
        //            foreach (string s in MatchText.Keys)
        //            {
        //                var pos = text.IndexOf(s);
        //                if (pos == -1)
        //                {
        //                    continue;
        //                }
        //            }

        //            Write(text, nl);
        //        }
        //    });
        //}

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Clear()
        {
            rtbOut.Clear();
        }




        /// <summary>
        /// Low level output. Assumes caller has taken care of cross-thread issues.
        /// </summary>
        void Write(string text, bool eol = true)
        {
            // Trim buffer.
            if (rtbOut.TextLength > MaxText)
            {
                int end = MaxText / 5;
                while (rtbOut.Text[end] != LINEFEED) end++;
                rtbOut.Select(0, end);
                rtbOut.SelectedText = "";
            }

            rtbOut.AppendText(text);
            if (eol)
            {
                rtbOut.AppendText(Environment.NewLine);
            }
            rtbOut.ScrollToCaret();
        }




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




        // How windows handles key presses. For example Shift+A produces:
        // •   KeyDown: KeyCode=Keys.ShiftKey, KeyData=Keys.ShiftKey | Keys.Shift, Modifiers=Keys.Shift
        // •   KeyDown: KeyCode=Keys.A, KeyData=Keys.A | Keys.Shift, Modifiers=Keys.Shift
        // •   KeyPress: KeyChar='A'
        // •   KeyUp: KeyCode=Keys.A
        // •   KeyUp: KeyCode=Keys.ShiftKey
        // 
        // Also note that Windows steals TAB, RETURN, ESC, and arrow keys so they are not currently implemented.



        // D0 = 0x30, // 0
        // D9 = 0x39, // 9
        // A = 0x41,
        // Z = 0x5A,
        // Space = 0x20,



        /// <summary>
        /// Send all keystrokes to the cli.
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
                        while (_queue.TryDequeue(out CliInputEventArgsXXX? le))
                        {
                            // Do the work...

                            //bool running = true;
                            string? ucmd = null;
                            OpStatus res;
                            bool ok = true;

                            switch (le.Mod, le.Text)
                            {
                                case (ModifierXXX.None, _):
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

                                case (ModifierXXX.Alt, _):
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
            _logColors.TryGetValue(e.Level, out int color);
            Write($"\u001b[{color}m{e.Message}\u001b[0m");
        }

        /// <summary>
        /// Write to user.
        /// </summary>
        /// <param name="s"></param>
        void Write(string s)
        {
            //if (_settings.AnsiColor)
            //{
            //    tvOut.AppendAnsi(s);
            //}
            //else
            //{
            //    tvOut.Append(s);
            //}
        }

        /// <summary>
        /// 
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
