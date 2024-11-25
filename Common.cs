using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public Color FromAnsi(string ansi) // TODO
        {
            Color ret = Color.White;



            //            int[] _ansiColorMap = new[256];

            /*
            All the codes: https://gist.github.com/fnky/458719343aabd01cfb17a3a4f7296797
            "C:\Users\cepth\OneDrive\OneDriveDocuments\tech\ansi-terminal.lua"

            ESC = \033 or \X1B

            cat:
            ESC[1;31m  # Set style to bold, red foreground.


            Modes- probably not
            -----------
            set     reset
            ESC[1m  ESC[22m bold mode.
            ESC[2m  ESC[22m dim / faint mode.
            ESC[3m  ESC[23m italic mode.
            ESC[4m  ESC[24m underline mode.
            ESC[5m  ESC[25m blinking mode
            ESC[7m  ESC[27m inverse / reverse mode
            ESC[8m  ESC[28m hidden / invisible mode
            ESC[9m  ESC[29m strikethrough mode.

            8/16 Colors "ESC[94NNm"
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

            256 Colors
            -----------
            Set FG: ESC[38;5;INDEXm
            Set BG: ESC[48;5;INDEXm

            The table starts with the original 16 colors (0-15).
            The next 216 colors (16-231) or formed by a 3bpc RGB value offset by 16, packed into a single value.
            The final 24 colors (232-255) are grayscale starting from a shade slighly lighter than black,
            ranging up to shade slightly darker than white.

            Apart from colors, and background-colors, Ansi escape codes also allow decorations on the text:

            Bold: ESC[1m
            Underline: ESC[4m
            Reversed: ESC[7m
            Which can be used individually:

            "ESC[1m BOLD ESC[0mESC[4m Underline ESC[0mESC[7m Reversed ESC[0m"

            ----
            The majority of ANSI escape codes for 256 colors are 216 entries that are directly mapped onto a
            (6×6×6) RGB colorspace cube. These colors can usually be enabled by their decimal number as terminal
            ASCII escape code. (For example, see lolcat.)

            \x1b[38;5;…m, \033[38;5;…m – foreground
            \x1b[48;5;…m, \033[48;5;…m – background
            \x1b[0m, \033[0m – clear

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
