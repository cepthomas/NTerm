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


using System.Threading.Tasks.Dataflow;


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
        string _commConfig = "?";

        /// <summary>Client comm flavor.</summary>
        IComm? _comm = null;

        /// <summary>User hot keys.</summary>
        readonly Dictionary<char, string> _hotKeys = [];

        /// <summary>Cli event queue.</summary>
        readonly ConcurrentQueue<CliInput> _qCli = new();

        /// <summary>For timing measurements.</summary>
        readonly TimeIt _tmit = new();
        #endregion


        string _name = "???";

        string commType = "?";

        CommType _commType = CommType.Null;

        int _responseTime = 1000;

        Dictionary<string, ConsoleColorEx> _matchers = [];

        Progress<string> _progress = new();



        /// <summary>Build me one.</summary>
        public App()
        {
            //TimeIt.Snap("App()");
            bool ok = true;

            var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            InitFromSettings();

            // Set up log first.
            var logFileName = Path.Combine(appDir, "log.txt");
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => { Print($"{e.Message}"); DoPrompt(); };


            // Init stuff.
            LoadIni();

            _progress.ProgressChanged += (_, s) => { Print(s, false); };


            if (ok)
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
                        var stat = _comm.Send(le.Text);
                        // _logger.Debug($"Send took {Utils.GetCurrentMsec() - start}");

                        switch (stat)
                        {
                            case OpStatus.Success:
                    //            Print(resp);
                                break;

                            case OpStatus.Error:
                    //              _logger.Error($"Comm.Send() error [{msg}]");
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
        /// 
        /// </summary>
        void LoadIni()
        {
            // Process command line options.
            try
            {
                var args = Environment.GetCommandLineArgs();

                // cmd line: my.ini OR one of the comm_type args
                //[nterm]
                //comm_type=null
                //comm_type=tcp 127.0.0.1 59120
                //comm_type=udp 127.0.0.1 59120
                //comm_type=serial COM1 9600 8N1 ; E-O-N  6-7-8  0-1-15
                //[hot keys]
                //k=do something
                //o=send me
                //[matchers]
                //blue=Text to match

                for (int i = 0; i < args.Length; i++)
                {
                    var arg = args[i];

                    switch (arg)
                    {
                        case "null":
                            _commType = CommType.Null;
                            break;

                        case "tcp":
                            _commType = CommType.Tcp;
                            break;

                        case "udp":
                            _commType = CommType.Udp;
                            break;

                        case "serial":
                            _commType = CommType.Serial;
                            break;

                        case "-?":
                            Help();
                            Environment.Exit(0);
                            break;

                        default:
                            // If first, check for valid file.
                            if (i == 0)
                            {
                                if (arg.EndsWith(".ini") && File.Exists(arg))
                                {
                                    ReadIniFile(arg);
                                }
                                else
                                {
                                    throw new ArgumentException($"Invalid ini file: {arg}");
                                }
                            }
                            else
                            {
                                throw new ArgumentException($"Invalid argument: {arg}");
                            }
                            break;
                    }
                }
            }
            catch (IniSyntaxException ex)
            {
                Print($"IniSyntaxException at {ex.LineNum}: {ex.Message}");//, TODO errColor);
                Environment.Exit(1);
            }
            catch (ArgumentException ex)
            {
                Print($"ArgumentException: {ex.Message}");//, errColor);
                Environment.Exit(3);
            }
            catch (Exception ex)
            {
                Print($"{ex.GetType()}: {ex.Message}");//, errColor);
                Environment.Exit(4);
            }

            ///
            void ReadIniFile(string fn)
            {
                var inrdr = new IniReader(fn);

                foreach (var val in inrdr.Contents["nterm"].Values)
                {
                    switch (val.Key)
                    {
                        case "comm_type": commType = val.Value; break;
                        default: throw new IniSyntaxException($"Invalid section value for {val.Key}", -1);
                    }
                }

                //[hot_keys]
                //k=do something
                //o=send me
                foreach (var val in inrdr.Contents["hot_keys"].Values)
                {
                    _hotKeys[val.Key[0]] = val.Value; 
                }

                //[matchers]
                //blue=Text to match
                foreach (var val in inrdr.Contents["matchers"].Values)
                {
                    _matchers[val.Key] = Enum.Parse<ConsoleColorEx>(val.Value, true);
                }
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
            foreach (var m in _matchers)
            {
                if (text.Contains(m.Key))
                {
                    if (m.Value is not ConsoleColorEx.None) { Console.ForegroundColor = (ConsoleColor)m.Value; }
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
        void Help() // TODO
        {
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

        /// <summary>
        /// Load persisted settings.
        /// </summary>
        void InitFromSettings()
        {
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
        }
    }
}
