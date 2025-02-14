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
        readonly ConcurrentQueue<CliInput> _queue = new();

        /// <summary>Queue management.</summary>
        bool _running = false;

        /// <summary>Queue management.</summary>
        readonly CancellationTokenSource _tokenSource = new();

        /// <summary>Limit the size.</summary>
        int _maxText = 10000;
        #endregion


        enum Modifier { None, Ctrl, Alt }

        /// <summary>Internal data container.</summary>
        record CliInput(Modifier Mod, string Text);

        /// <summary>Ansi parse state.</summary>
        AnsiParseState _state = AnsiParseState.Idle;
        enum AnsiParseState { Idle, LookForBracket, CollectSequence }

        /// <summary>Accumulated ansi arguments.</summary>
        string _ansiArgs = "";

        /// <summary>Most recent at beginning.</summary>
        List<string> _history = [];

        /// <summary>Current location in list.</summary>
        int _historyIndex = 0;


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
            LogManager.LogMessage += LogMessage;

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
            btnDebug.Click += (_, _) => DoRegex();
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


        void DoRegex() // TODO 
        {
            /*
            \x1b[34;01m
            "\u001B"
            [38;5;32m
            (\u001B\[\d+;\d+m)+

            section-heading:
            - match: ^(#+ +[^\[]+) *(?:\[(.*)\])?\n

            link definition <name>(link)
            - match: <([^>)]+)>\(([^\)]+)\)
            captures:
            1: markup.link.name.notr
            2: markup.link.target.notr
            3: markup.link.tags.notr
            */

            // https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.group.captures

            string pattern = @"([^\u001B]+)\u001B\[([^m)]+)m";
            string input = "No color [31m Standard colors [38;5;32m  256 Colors [38;2;60;120;180m  RGB Colors [0m reset";
            //>>>
            //Matched text: No color [31m [0 14]
            //  Group 0: No color [31m
            //    Capture 1 :  No color [31m
            //  Group 1: No color 
            //    Capture 2 :  No color 
            //  Group 2: 31
            //    Capture 3 :  31
            //Matched text:  Standard colors [38;5;32m [14 27]
            //  Group 0:  Standard colors [38;5;32m
            //    Capture 1 :   Standard colors [38;5;32m
            //  Group 1:  Standard colors 
            //    Capture 2 :   Standard colors 
            //  Group 2: 38;5;32
            //    Capture 3 :  38;5;32
            //Matched text:   256 Colors [38;2;60;120;180m [41 31]
            //  Group 0:   256 Colors [38;2;60;120;180m
            //    Capture 1 :    256 Colors [38;2;60;120;180m
            //  Group 1:   256 Colors 
            //    Capture 2 :    256 Colors 
            //  Group 2: 38;2;60;120;180
            //    Capture 3 :  38;2;60;120;180
            //Matched text:   RGB Colors [0m [72 17]
            //  Group 0:   RGB Colors [0m
            //    Capture 1 :    RGB Colors [0m
            //  Group 1:   RGB Colors 
            //    Capture 2 :    RGB Colors 
            //  Group 2: 0
            //    Capture 3 :  0


            bool mult = true;
            if (mult)
            {
                int lastIndex = 0;

                var matches = Regex.Matches(input, pattern);

                foreach (Match match in Regex.Matches(input, pattern))
                {
                    lastIndex = match.Index + match.Length;

                    Write($"Matched text: {match.Value} [{match.Index} {match.Length}]");

                    int groupCtr = 0;

                    foreach (Group group in match.Groups)
                    {
                        Write($"  Group {groupCtr}: {group.Value}");
                        groupCtr++;

                        int capCtr = 0;
                        foreach (Capture capture in group.Captures)
                        {
                            Write($"    Capture {groupCtr} :  {group.Value}");
                            capCtr++;
                        }
                    }
                }

                var dangling = input.Substring(lastIndex);
                Write($"  Dangling : [{dangling}]");
            }


            //string pattern = @"([^<]+)<([^>)]+)>\(([^\)]+)\)";
            //string input = "1111 <name1>(lnk1)  2222 <name2>(lnk2)  3333 <name3>(lnk3)";
            //>>>
            //Matched text: 1111 <name1>(lnk1)
            //  Group 1: 1111 
            //    Capture 0: 1111 
            //  Group 2: name1
            //    Capture 0: name1
            //  Group 3: lnk1
            //    Capture 0: lnk1
            //Matched text:   2222 <name2>(lnk2)
            //  Group 1:   2222 
            //    Capture 0:   2222 
            //  Group 2: name2
            //    Capture 0: name2
            //  Group 3: lnk2
            //    Capture 0: lnk2


            //string pattern = @"\b(\w+\s*)+\.";
            //string input = "This is a sentence. This is another sentence.";
            //>>>
            //Matched text: This is a sentence.
            //  Group 1: sentence
            //    Capture 0: This 
            //    Capture 1: is 
            //    Capture 2: a 
            //    Capture 3: sentence
            //Matched text: This is another sentence.
            //  Group 1: sentence
            //    Capture 0: This 
            //    Capture 1: is 
            //    Capture 2: another 
            //    Capture 3: sentence

            // 1st Capturing Group (\w+\s*)+
            //   + matches the previous token between one and unlimited times, as many times as possible, giving back as needed (greedy)
            //   \w match any word character in any script (equivalent to [\p{L}\p{Mn}\p{Nd}\p{Pc}])
            //   + matches the previous token between one and unlimited times, as many times as possible, giving back as needed (greedy)
            //   \s matches any kind of invisible character (equivalent to [\f\n\r\t\v\p{Z}])
            //   * matches the previous token between zero and unlimited times, as many times as possible, giving back as needed (greedy)
            // \. matches the character . with index 4610 (2E16 or 568) literally (case sensitive)


            //string input = "QSMDRYCELL   11.00   11.10   11.00   11.00    -.90      11     11000     1.212";
            //string pattern = @"^(\S+)\s+(\s+[\d.-]+){8}$";
            // >>>
            //Matched text: QSMDRYCELL   11.00   11.10   11.00   11.00 - .90      11     11000     1.212
            //   Group 1: QSMDRYCELL
            //      Capture 0: QSMDRYCELL
            //   Group 2:      1.212
            //      Capture 0:  11.00
            //      Capture 1:    11.10
            //      Capture 2:    11.00
            //      Capture 3:    11.00
            //      Capture 4:     -.90
            //      Capture 5:       11
            //      Capture 6:      11000
            //      Capture 7:      1.212


            // string pattern = @"\b(\w+)\b";
            // string input = "This is one sentence.";
            // >>>
            //       Matched text: This
            //          Group 1:  This
            //             Capture 0: This


            //// extract and clean up port name and number
            //c.name = p.GetPropertyValue("Name").ToString();
            //Match mName = Regex.Match(c.name, namePattern);
            //if (mName.Success)
            //{
            //    c.name = mName.Value;
            //    c.num = int.Parse(c.name.Substring(3));
            //}

            //// if the port name or number cannot be determined, skip this port and move on
            //if (c.num < 1)
            //{
            //    continue;
            //}

            //// get the device's VID and PID
            //string pidvid = p.GetPropertyValue("PNPDeviceID").ToString();

            //// extract and clean up device's VID
            //Match mVID = Regex.Match(pidvid, vidPattern, RegexOptions.IgnoreCase);
            //if (mVID.Success)
            //{
            //    c.vid = mVID.Groups[1].Value.Substring(0, Math.Min(4, c.vid.Length));
            //}
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
                    if (_tokenSource.Token.IsCancellationRequested)
                    {
                        _running = false;
                    }

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

        #region Key handlers TODO
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RtbOut_KeyDown(object? sender, KeyEventArgs e)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RtbIn_KeyDown(object? sender, KeyEventArgs e)
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
        #endregion

        #region Output text
        /// <summary>
        /// Output text wwith ansi encoding.    
        /// </summary>
        /// <param name="text">The text to show.</param>
        /// <param name="nl">Add new line.</param>
        void AppendAnsi(string text, bool nl = true)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                switch (_state, c)
                {
                    case (AnsiParseState.Idle, (char)Keys.Escape):
                        _state = AnsiParseState.LookForBracket;
                        break;

                    case (AnsiParseState.Idle, (char)Keys.Return):
                        _ansiArgs = "";
                        Write("");
                        break;

                    case (AnsiParseState.LookForBracket, '['):
                        _ansiArgs = "";
                        _state = AnsiParseState.CollectSequence;
                        break;

                    case (AnsiParseState.CollectSequence, 'm'):
                        var (fg, bg) = ColorFromAnsi(_ansiArgs);
                        rtbOut.SelectionColor = fg;
                        rtbOut.SelectionBackColor = bg;
                        _state = AnsiParseState.Idle;
                        _ansiArgs = "";
                        break;

                    case (AnsiParseState.CollectSequence, _):
                        _ansiArgs += c;
                        break;

                    // TODO these useful?
                    //case (ParseState.Idle, CANCEL):
                    //case (ParseState.Idle, BACKSPACE):
                    //case (ParseState.Idle, TAB):
                    //case (ParseState.Idle, LINEFEED):
                    //case (ParseState.Idle, CLEAR):
                    //    break;

                    case (AnsiParseState.Idle, _):
                        Write(c.ToString(), false);
                        break;

                    case (_, _):
                        // Anything else is a syntax error.
                        Write($"ERROR<{_ansiArgs}", false);
                        _state = AnsiParseState.Idle;
                        break;
                }
            }

            Write("", nl);
        }

        //public void AppendColor(string text, Color? fg = null, Color? bg = null, bool nl = true)
        //{
        //    {
        //        rtbOut.SelectionColor = (Color)(fg == null ? rtbOut.ForeColor : fg);
        //        rtbOut.SelectionBackColor = (Color)(bg == null ? rtbOut.SelectionBackColor : bg);

        //        Write(text, nl);
        //    });
        //}

        /// <summary>
        /// Output text using text matching color.
        /// </summary>
        /// <param name="text">The text to show.</param>
        /// <param name="line">Light up whole line otherwise just the word.</param>
        /// <param name="nl">Add new line.</param>
        //public void AppendMatch(string text, bool line = true, bool nl = true)
        //{
        //    {
        //        //TODO use regex
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
        #endregion


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
        /// Low level output. Assumes caller has taken care of cross-thread issues.
        /// </summary>
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

                rtbOut.AppendText(text);
                if (nl)
                {
                    rtbOut.AppendText(Environment.NewLine);
                }
                rtbOut.ScrollToCaret();
            });

        }

        // /// <summary>
        // /// Write to user.
        // /// </summary>
        // /// <param name="s"></param>
        // void Write(string s)
        // {
        //     if (_settings.AnsiColor)
        //     {
        //        tvOut.AppendAnsi(s);
        //     }
        //     else
        //     {
        //        tvOut.Append(s);
        //     }
        // }

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
