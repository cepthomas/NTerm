using System.Text.Json;
using Ephemera.NBagOfTricks; // TODO update this.
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;


namespace NTerm
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>Settings</summary>
        readonly UserSettings _settings;

        /// <summary>Current config</summary>
        Config _config;

        /// <summary>Client flavor.</summary>
        IProtocol _client;

        /// <summary>My logger</summary>
        readonly Logger _logger = LogManager.CreateLogger("MainForm");
        #endregion

        #region Lifecycle
        /// <summary>
        /// Create the main form.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            string appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            StartPosition = FormStartPosition.Manual;
            Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);

            // Set up log.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = LogLevel.Trace;
            LogManager.MinLevelNotif = LogLevel.Trace;
            LogManager.Run(logFileName, 50000);
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => cliOut.AppendLine(e.Message);

            cliIn.InputEvent += CliIn_InputEvent;

            btnGo.Click += BtnGo_Click;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            // Open config.
            var args = Environment.GetCommandLineArgs();
            if (args.Length != 2)
            {
                cliOut.AppendLine("Invalid args. Restart please.");
                //Environment.Exit(1);
            }

            try
            {
                string json = File.ReadAllText(args[1]);
                object? set = JsonSerializer.Deserialize(json, typeof(Config));
                _config = (Config)set!;

                switch (_config.Protocol.ToLower())
                {
                    case "tcp":
                        _client = new TcpProtocol(_config.Host, _config.Port);
                        break;

                    default:
                        _logger.Error($"Invalid protocol: {_config.Protocol}");
                        break;
                }
            }
            catch (Exception ex)
            {
                // Errors are considered fatal.
                _logger.Error($"Invalid config {args[1]}:{ex}");
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _settings.FormGeometry = new Rectangle(Location.X, Location.Y, Size.Width, Size.Height);
            _settings.Save();

            base.OnFormClosing(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                _client?.Dispose();
                components.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        /// <summary>
        /// Do test stuff.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BtnGo_Click(object? sender, EventArgs e)
        {
            try
            {
                // Works:
                //AsyncUsingTcpClient();

                //StartServer(_config.Port);
                var res = _client.Send("djkdjsdfksdf;s");
                cliOut.AppendLine($"{res}: {_client.Response}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Fatal error: {ex.Message}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CliIn_InputEvent(object? sender, TermInputEventArgs e)
        {
            OpStatus res = OpStatus.Success;

            if (e.Line is not null)
            {
                _logger.Trace($"SND:{e.Line}");
                res = _client.Send(e.Line);
                e.Handled = true;
            }
            else if (e.HotKey is not null)  // single key
            {
                // If it's in the hotkeys send it now
                var hk = (char)e.HotKey;
                if (_config.HotKeys.Contains(hk))
                {
                    _logger.Trace($"SND:{hk}");
                    res = _client.Send(hk.ToString());
                    e.Handled = true;
                }
                else
                {
                    e.Handled = false;
                }
            }

            // Show results. TODO extract/convert ansi codes. Use regex.
            cliOut.AppendLine($"{res}: {_client.Response}");
            _logger.Trace($"RCV:{res}: {_client.Response}");
        }
    }
}
