using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using NTerm;


namespace NTermTest
{
    public class ANSI_COLOR : TestSuite // TODOF expand to all Print() functions.
    {
        public override void RunSuite()
        {
            UT_INFO("Tests ANSI color functions.");

            // ESC = \033
            // One of: ESC[IDm  ESC[38;5;IDm  ESC[48;5;IDm  ESC[38;2;R;G;Bm  ESC[48;2;R;G;Bm

            var (color, invert) = Ansi.ColorFromAnsi("bad string");
            UT_TRUE(color.IsEmpty);

            (color, invert) = Ansi.ColorFromAnsi("\033[34m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ff00007f");

            (color, invert) = Ansi.ColorFromAnsi("\033[45m");
            UT_TRUE(invert);
            UT_EQUAL(color.Name, "ff7f007f");

            // system
            (color, invert) = Ansi.ColorFromAnsi("\033[38;5;12m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ff0000ff");

            // id
            (color, invert) = Ansi.ColorFromAnsi("\033[38;5;122m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ff87ffd7");

            // grey
            (color, invert) = Ansi.ColorFromAnsi("\033[38;5;249m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ffb2b2b2");

            // id bg
            (color, invert) = Ansi.ColorFromAnsi("\033[48;5;231m");
            UT_TRUE(invert);
            UT_EQUAL(color.Name, "ffffffff");


            //ESC[38;2;R;G;Bm
            // rgb
            (color, invert) = Ansi.ColorFromAnsi("\033[38;2;204;39;187m");
            UT_FALSE(invert);
            UT_EQUAL(color.Name, "ffcc27bb");

            // rgb invert
            (color, invert) = Ansi.ColorFromAnsi("\033[48;2;19;0;222m");
            UT_TRUE(invert);
            UT_EQUAL(color.Name, "ff1300de");
        }
    }
}
