using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    /// <summary>Flavor.</summary>
    public enum CommType { None, Tcp, Serial }

    /// <summary>How did we do?</summary>
    public enum OpStatus { Success, Timeout, Error }

    /// <summary>Comm type: tcp/socket, stream, pipe, serial, ...</summary>
    public interface IComm : IDisposable
    {
        /// <summary>Server must connect or reply to commands in msec.</summary>
        int ResponseTime { get; set; }

        /// <summary>Optional R/W buffer size.</summary>
        int BufferSize { get; set; }

        /// <summary>The text if Success otherwise error message.</summary>
        string Response { get; }

        /// <summary>Send a message to the server.</summary>
        /// <param name="msg">What to send.</param>
        /// <returns>Operation status.</returns>
        OpStatus Send(string msg);
    }


    public class NullComm : IComm
    {
        #region IComm implementation
        public int ResponseTime { get; set; } = 500;
        public int BufferSize { get; set; } = 4096;
        public string Response { get; private set; } = "";
        public void Dispose() { }
        public OpStatus Send(string msg) { return OpStatus.Success; }
        #endregion
    }


    // #region Console abstraction to support testing
    // public interface IConsole
    // {
    //     bool KeyAvailable { get; }
    //     bool CursorVisible { get; set; }
    //     string Title { get; set; }
    //     int BufferWidth { get; set; }
    //     void Write(string text);
    //     void WriteLine(string text);
    //     string? ReadLine();
    //     ConsoleKeyInfo ReadKey(bool intercept);
    //     (int left, int top) GetCursorPosition();
    //     void SetCursorPosition(int left, int top);
    // }
    // #endregion




    /// <summary>What are we doing today?</summary>
    [Serializable]
    public class Config
    {
        [DisplayName("Name")]
        [Description("Name")]
        [Browsable(true)]
        public string Name { get; set; } ="???";

        [DisplayName("Communication Type")]
        [Description("Talk like this.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Browsable(true)]
        public CommType CommType { get; set; } = CommType.None;

        [DisplayName("Communication Arguments")] // TODO could get fancier later.
        [Description("Type specific args.\n\"127.0.0.1 59120\"\n\"COM1 115200 E-O-N 6-7-8 0-1-1.5\"")]
        [Browsable(true)]
        public string Args { get; set; } ="???";

        [DisplayName("Encoding")]
        [Description("Encoding.")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [Browsable(true)]
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        // /// <summary>TCP host.</summary>
        // public string Host { get; set; } ="???";

        // /// <summary>TCP port.</summary>
        // public int Port { get; set; } = 0;

        // /// <summary>UI function TODO.</summary>
        // public bool ShowStatus { get; set; } = false;

        [DisplayName("Hot Keys")]
        [Description("Hot key definitions.\n\"key command\"")] // like ctrl-k  alt+shift+o
        [Browsable(true)]
        public string HotKeys { get; set; } = "";
    }

    [Serializable]
    public sealed class UserSettings : SettingsCore
    {
        #region Properties - persisted editable
        [DisplayName("Configurations")]
        [Description("Your favorites.")]
        [Browsable(true)]
        public List<Config> Configs { get; set; } = new();

        [DisplayName("Open Last Config")]
        [Description("Open last config on start.")]
        [Browsable(true)]
        public bool OpenLastConfig { get; set; } = true;

        [DisplayName("Prompt")]
        [Description("CLI prompt.")]
        [Browsable(true)]
        public string Prompt { get; set; } = ">";

        [DisplayName("File Log Level")]
        [Description("Log level for file write.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;

        [DisplayName("Notification Log Level")]
        [Description("Log level for UI notification.")]
        [Browsable(true)]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;

        // [DisplayName("Background Color")]
        // [Description("The color used for overall background.")]
        // [Browsable(true)]
        // [JsonConverter(typeof(JsonColorConverter))]
        // public Color BackColor { get; set; } = Color.LightYellow;
        #endregion

        // #region Properties - internal
        // [Browsable(false)]
        // public bool WordWrap { get; set; } = false;

        // [Browsable(false)]
        // public bool MonitorRcv { get; set; } = false;

        // [Browsable(false)]
        // public bool MonitorSnd { get; set; } = false;
        // #endregion
    }

    public class Utils
    {
        /// <summary>
        /// Convert ansi specs like: ESC[IDm  ESC[38;5;IDm ESC[38;2;R;G;Bm to Color. TODO find better home. AnsiFromColor?
        /// </summary>
        /// <param name="ansi">Ansi string</param>
        /// <returns>Color and whether it's fg or bg. Color is Empty if invalid ansi string.</returns>
        public static (Color color, bool invert) ColorFromAnsi(string ansi)
        {
            Color color = Color.Empty;
            bool invert = false;

            try
            {
                var shansi = ansi.Replace("\033[", "").Replace("m", "");
                var parts = shansi.SplitByToken(";").Select(i => int.Parse(i)).ToList();

                var p0 = parts.Count >= 1 ? parts[0] : 0;
                var p1 = parts.Count >= 2 ? parts[1] : 0;
                var p2 = parts.Count == 3 ? parts[2] : 0;

                switch (parts.Count)
                {
                    /////// Standard 8/16 colors. ESC[IDm
                    case 1 when p0 >= 30 && p0 <= 37:
                        color = MakeStdColor(p0 - 30);
                        break;

                    case 1 when p0 >= 40 && p0 <= 47:
                        color = MakeStdColor(p0 - 40);
                        invert = true;
                        break;

                    case 1 when p0 >= 90 && p0 <= 97:
                        color = MakeStdColor(p0 - 90);
                        break;

                    case 1 when p0 >= 100 && p0 <= 107:
                        color = MakeStdColor(p0 - 100);
                        invert = true;
                        break;

                    /////// 256 colors. ESC[38;5;IDm  ESC[48;5;IDm
                    case 3 when (p0 == 38 || p0 == 48) && p1 == 5 && p2 >= 0 && p2 <= 15:
                        // 256 colors - standard color.
                        color = MakeStdColor(p2);
                        invert = p0 == 48;
                        break;

                    case 3 when (p0 == 38 || p0 == 48) && p1 == 5 && p2 >= 16 && p2 <= 231:
                        // 256 colors - rgb color.
                        int[] map6 = [0, 95, 135, 175, 215, 255];
                        int im = p2 - 16;
                        int r = map6[im / 36];
                        int g = map6[(im / 6) % 6];
                        int b = map6[im % 6];
                        color = Color.FromArgb(r, g, b);
                        invert = p0 == 48;
                        break;

                    case 3 when (p0 == 38 || p0 == 48) && p1 == 5 && p2 >= 232 && p2 <= 255:
                        // 256 colors - grey
                        int i = p2 - 232; // 0 - 23
                        int grey = i * 10 + 8;
                        color = Color.FromArgb(grey, grey, grey);
                        invert = p0 == 48;
                        break;

                    /////// Explicit rgb colors. ESC[38;2;R;G;Bm  ESC[48;2;R;G;Bm
                    case 5 when p0 == 38 || p0 == 48 && p1 == 2:
                        color = Color.FromArgb(parts[2], parts[3], parts[4]);
                        invert = p0 == 48;
                        break;
                }
            }
            catch
            {
                // Indicate failure.
                color = Color.Empty;
            }

            return (color, invert);

            static Color MakeStdColor(int id)
            {
                (int r, int g, int b)[] std_colors = [
                    (0, 0, 0), (127, 0, 0), (0, 127, 0), (127, 127, 0), (0, 0, 127), (127, 0, 127), (0, 127, 127), (191, 191, 191),
                    (127, 127, 127), (0, 0, 0), (0, 255, 0), (255, 255, 0), (0, 0, 255), (255, 0, 255), (0, 255, 255), (255, 255, 255)];
                return Color.FromArgb(std_colors[id].r, std_colors[id].g, std_colors[id].b);
            }
        }
    }
}
