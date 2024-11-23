using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTerm
{
    // Interface to tcp/socket, stream, pipe
    public interface IProtocol : IDisposable
    {
        //bool KeyAvailable { get; }
        //bool CursorVisible { get; set; }
        //string Title { get; set; }
        //int BufferWidth { get; set; }
        //void Write(string text);

        string? Send(string cmd);

        //string? ReadLine();
        
        //ConsoleKeyInfo ReadKey(bool intercept);
        //(int left, int top) GetCursorPosition();
        //void SetCursorPosition(int left, int top);
    }
    // The Transport Layer (Layer 4)
    // Layer 4 of the OSI model is named the transport layer and is responsible for message segmentation,
    // acknowledgement, traffic control, and session multiplexing. The transport layer also has the ability
    // to perform error detection and correction (resends), message reordering to ensure message sequence,
    // and reliable message channel depending on the specific transport layer protocol used. The most common
    // of the used transport layer protocols include the Transport Control Protocol (TCP) and
    // User Datagram Protocol (UDP).

    // The Session Layer (Layer 5)
    // Layer 5 of the OSI model is named the session layer and is responsible for session establishment,
    // maintenance and termination (the ability to have multiple devices use a single application from
    // multiple locations). Common examples of session layer protocols are Named Pipes and NetBIOS.


    // Console abstraction to support testing
    public interface IConsole
    {
        bool KeyAvailable { get; }
        bool CursorVisible { get; set; }
        string Title { get; set; }
        int BufferWidth { get; set; }
        void Write(string text);
        void WriteLine(string text);
        string? ReadLine();
        ConsoleKeyInfo ReadKey(bool intercept);
        (int left, int top) GetCursorPosition();
        void SetCursorPosition(int left, int top);
    }



    internal class NewStuff
    {
        public Color FromAnsi(string ansi)
        {
            Color ret = Color.White;

//            int[] _ansiColorMap = new[256];


// 30-39 40-49  90-97 100-107
// ESC[1m  ESC[22m set bold mode.
// ESC[2m  ESC[22m set dim/faint mode.
// ESC[3m  ESC[23m set italic mode.
// ESC[4m  ESC[24m set underline mode.
// ESC[5m  ESC[25m set blinking mode
// ESC[7m  ESC[27m set inverse/reverse mode
// ESC[8m  ESC[28m set hidden/invisible mode
// ESC[9m  ESC[29m set strikethrough mode.

// 8-16 Colors
// Color Name  Foreground Color Code   Background Color Code
// Black   30  40
// Red 31  41
// Green   32  42
// Yellow  33  43
// Blue    34  44
// Magenta 35  45
// Cyan    36  46
// White   37  47
// Default 39  49
// Bright Black    90  100
// Bright Red  91  101
// Bright Green    92  102
// Bright Yellow   93  103
// Bright Blue 94  104
// Bright Magenta  95  105
// Bright Cyan 96  106
// Bright White    97  107

/*
-- All the codes: https://gist.github.com/fnky/458719343aabd01cfb17a3a4f7296797

io.write("16 Colors\n")

for i = 0, 108 do
    r = i / 10
    c = i % 10

    -- s = string.format("\033[%dm %3d\033[m", i, i)
    s = string.format("[%dm %3d[m", i, i)

    io.write(s)
    if c == 9 then io.write("\n") end

end
io.write("\n\n")


io.write("256 Colors\n")

-- 256 Colors
-- The table starts with the original 16 colors (0-15).
-- The proceeding 216 colors (16-231) or formed by a 3bpc RGB value offset by 16, packed into a single value.
-- The final 24 colors (232-255) are grayscale starting from a shade slighly lighter than black,
-- ranging up to shade slightly darker than white.

-- The following escape codes tells the terminal to use the given color ID:
-- ESC Code Sequence   Description
-- ESC[38;5;{ID}m  Set foreground color.
-- ESC[48;5;{ID}m  Set background color.

for i = 0, 255 do
    r = i / 10
    c = i % 10

    s = string.format("[38;5;%dm %3d[m", i, i)

    io.write(s)
    if c == 9 then io.write("\n") end

end
io.write("\n")
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
