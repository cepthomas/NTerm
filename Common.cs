using Ephemera.NBagOfTricks;
using System;
using System.Drawing;
using System.Linq;


namespace NTerm
{
    public static class Defs
    {
       // public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        // or
        public const nint INVALID_HANDLE_VALUE = -1; // since C# 9
    }

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
        /// <param name="cmd">What to send.</param>
        /// <returns>Operation status.</returns>
        OpStatus Send(string cmd);
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
        /// <summary>Talk like this.</summary>
        public string Protocol { get; set; } = "???";

        /// <summary>TCP host.</summary>
        public string Host { get; set; } ="???";

        /// <summary>TCP port.</summary>
        public int Port { get; set; } = 0;

        /// <summary>UI function TODO.</summary>
        public bool ShowStatus { get; set; } = false;

        /// <summary>Hot key defs.</summary>
        public string HotKeys { get; set; } = "";
    }

    public class Utils
    {
        /// <summary>
        /// Convert ansi specs like: ESC[IDm  ESC[38;5;IDm ESC[38;2;R;G;Bm to Color. TODO find better home.
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
            catch (Exception e)
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
