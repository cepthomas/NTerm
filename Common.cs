using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    public enum OpStatus { Success, Timeout, Error }

    /// <summary>Interface to tcp/socket, stream, pipe, serial, ...</summary>
    public interface IProtocol : IDisposable
    {
        /// <summary>Server must connect or reply to commands in msec.</summary>
        int ResponseTime { get; set; }

        /// <summary>Optional R/W buffer size.</summary>
        int BufferSize { get; set; }

        /// <summary>The text if Success otherwise error message.</summary>
        string Response { get; }

        /// <summary>
        /// Send a message to the server.
        /// </summary>
        /// <param name="cmd">What to send.</param>
        /// <returns>Operation status.</returns>
        OpStatus Send(string cmd);
    }

    internal class Utils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ansi">One of NN  38;5;INDEX  38;2;R;G;B</param>
        /// <returns></returns>
        public Color ColorFromAnsi(string ansi) // TODO
        {
            // Client searches for strings starting with "ESC[" and ending with "m".
            // ESC[NNm  ESC[38;5;INDEXm  ESC[38;2;R;G;Bm
            // One of NN  38;5;INDEX  38;2;R;G;B   48;5;INDEX  48;2;R;G;B

            Color color = Color.Empty;

            var parts = ansi.SplitByToken(";").Select(i => int.Parse(i)).ToList();


            //You can just use the Select() extension method:

            //IEnumerable<int> integers = new List<int>() { 1, 2, 3, 4, 5 };
            //IEnumerable<string> strings = integers.Select(i => i.ToString());
            //Or in LINQ syntax:

            //IEnumerable<int> integers = new List<int>() { 1, 2, 3, 4, 5 };

            //var strings = from i in integers
            //            select i.ToString();
            //var parts = ansi.SplitByToken(";").ForEach(p => { int.Parse(p.Trim()); });

            (int r, int g, int b)[] std_colors = [
                (0, 0, 0), (128, 0, 0), (0, 128, 0), (128, 128, 0), (0, 0, 128), (128, 0, 128), (0, 128, 128), (192, 192, 192),
                (128, 128, 128), (0, 0, 0), (0, 255, 0), (255, 255, 0), (0, 0, 255), (255, 0, 255), (0, 255, 255), (255, 255, 255)];

            // One of NN  38;5;INDEX   48;5;INDEX   38;2;R;G;B   48;2;R;G;B
            int code = parts[0];
            var index = parts.Count == 3 ? parts[2] : 0;

            switch (parts.Count())
            {
                case 1 when code >= 0 && code <= 15:
                    // Standard color.

                    //Color          FG  BG 
                    //Black          30  40
                    //Red            31  41
                    //Green          32  42
                    //Yellow         33  43
                    //Blue           34  44
                    //Magenta        35  45
                    //Cyan           36  46
                    //White          37  47
                    //Default        39  49

                    //Bright Black   90  100
                    //Bright Red     91  101
                    //Bright Green   92  102
                    //Bright Yellow  93  103
                    //Bright Blue    94  104
                    //Bright Magenta 95  105
                    //Bright Cyan    96  106
                    //Bright White   97  107

                    color = Color.FromArgb(std_colors[code].r, std_colors[code].g, std_colors[code].b);
                    break;

                case 3 when (code == 38 || code == 48) && code >= 0 && code <= 15:
                    // 256 colors - standard color.
                    color = Color.FromArgb(std_colors[code].r, std_colors[code].g, std_colors[code].b);
                    break;

                case 3 when (code == 38 || code == 48) && code >= 16 && code <= 231:
                    // 256 colors - rgb color.


                    //map6 = {0, 95, 135, 175, 215, 255}
                    //for i = 16, 231 do
                    //    im = i - 16
                    //    r = map6[im // 36 + 1]
                    //    g = map6[(im // 6) % 6 + 1]
                    //    b = map6[im % 6 + 1]

                    //    -- FG=38  BG=48
                    //    s = string.format("[48;2;%d;%d;%dm %3d[m", r, g, b, i)

                    //color = Color.FromArgb(std_colors[code].r, std_colors[code].g, std_colors[code].b);
                    break;

                case 3 when (code == 38 || code == 48) && code >= 232 && code <= 255:
                    // 256 colors - grey color.
                    color = Color.FromArgb(code - 232, code - 232, code - 232);
                    break;

                case 5 when code == 38 || code == 48:
                    break;


            }




            /*
            All the codes: https://gist.github.com/fnky/458719343aabd01cfb17a3a4f7296797
            "C:\Users\cepth\OneDrive\OneDriveDocuments\tech\ansi-terminal.lua"
            ESC = \033 or \X1B
            concat: ESC[1;31m  # Set style to bold, red foreground.

            >>> 8/16 Colors "ESC[NNm"
            -----------
            Color          FG  BG 
            Black          30  40
            Red            31  41
            Green          32  42
            Yellow         33  43
            Blue           34  44
            Magenta        35  45
            Cyan           36  46
            White          37  47
            Default        39  49
            Bright Black   90  100
            Bright Red     91  101
            Bright Green   92  102
            Bright Yellow  93  103
            Bright Blue    94  104
            Bright Magenta 95  105
            Bright Cyan    96  106
            Bright White   97  107

            >>> 256 Colors by index
            -----------
            Set FG: ESC[38;5;INDEXm
            Set BG: ESC[48;5;INDEXm

            The table starts with the original 16 colors (0-15).
            The next 216 colors (16-231) or formed by a 3bpc RGB value offset by 16, packed into a single value.
            The final 24 colors (232-255) are grayscale starting from a shade slighly lighter than black,
            ranging up to shade slightly darker than white.


            >>> 256 Colors by RGB
            -----------

            FG: ESC[38;2;R;G;Bm
            BG: ESC[48;2;R;G;Bm

            **** system 0-15

            0    ,Black            ,#000000,0  ,0  ,0  
            1    ,Maroon           ,#800000,128,0  ,0  
            2    ,Green            ,#008000,0  ,128,0  
            3    ,Olive            ,#808000,128,128,0  
            4    ,Navy             ,#000080,0  ,0  ,128
            5    ,Purple           ,#800080,128,0  ,128
            6    ,Teal             ,#008080,0  ,128,128
            7    ,Silver           ,#c0c0c0,192,192,192
            8    ,Grey             ,#808080,128,128,128
            9    ,Red              ,#ff0000,0  ,0  ,0  
            10   ,Lime             ,#00ff00,0  ,255,0  
            11   ,Yellow           ,#ffff00,255,255,0  
            12   ,Blue             ,#0000ff,0  ,0  ,255
            13   ,Fuchsia          ,#ff00ff,255,0  ,255
            14   ,Aqua             ,#00ffff,0  ,255,255
            15   ,White            ,#ffffff,255,255,255


            **** 16-231

            map6 = {0, 95, 135, 175, 215, 255}
            for i = 16, 231 do
                im = i - 16
                if im % 20 == 0 then io.write("\n") end
                r = map6[im // 36 + 1]
                g = map6[(im // 6) % 6 + 1]
                b = map6[im % 6 + 1]


            **** greys 232-255

            i = index - 232
            r = g = b = 10 * i + 8




            -----------
            Apart from colors, and background-colors, Ansi escape codes also allow decorations on the text:

            Bold: ESC[1m
            Underline: ESC[4m
            Reversed: ESC[7m
            Which can be used individually:

            "ESC[1m BOLD ESC[0mESC[4m Underline ESC[0mESC[7m Reversed ESC[0m"

            set     reset
            ESC[1m  ESC[22m bold mode.
            ESC[2m  ESC[22m dim / faint mode.
            ESC[3m  ESC[23m italic mode.
            ESC[4m  ESC[24m underline mode.
            ESC[5m  ESC[25m blinking mode
            ESC[7m  ESC[27m inverse / reverse mode
            ESC[8m  ESC[28m hidden / invisible mode
            ESC[9m  ESC[29m strikethrough mode.


            More modern terminals supports Truecolor (24-bit RGB), which allows you to set foreground and
            background colors using RGB. These escape sequences are usually not well documented.
            ESC Code Sequence       Description
            ESC[38;2;{r};{g};{b}m   Set foreground color as RGB.
            ESC[48;2;{r};{g};{b}m   Set background color as RGB.
            Note that ;38 and ;48 corresponds to the 16 color sequence and is interpreted by the terminal
            to set the foreground and background color respectively. Whereas ;2 and ;5 sets the color format.

            */

            return ret;
        }

        public int DecodeKey(Keys key)
        {

            //Shift = 0x00010000,
            //Control = 0x00020000,
            //Alt = 0x00040000,
            //D0 = 0x30, // 0
            //A = 0x41,
            //F1 = 0x70,


            return 0;
        }
    }
}
