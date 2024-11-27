using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using static NTerm.Utils;


namespace NTermTest
{
    public class Ansi
    {
        public static void Run()
        {
            // ESC = \033
            // One of: ESC[IDm  ESC[38;5;IDm  ESC[48;5;IDm  ESC[38;2;R;G;Bm  ESC[48;2;R;G;Bm

            var (color, invert) = ColorFromAnsi("bad string");
            Debug.Assert(color == Color.Empty);

            (color, invert) = ColorFromAnsi("\033[34m");
            Debug.Assert(!invert);
            Debug.Assert(color.Name == "ff00007f");

            (color, invert) = ColorFromAnsi("\033[45m");
            Debug.Assert(invert);
            Debug.Assert(color.Name == "ff7f007f");

            // system
            (color, invert) = ColorFromAnsi("\033[38;5;12m");
            Debug.Assert(!invert);
            Debug.Assert(color.Name == "ff0000ff");

            // id
            (color, invert) = ColorFromAnsi("\033[38;5;122m");
            Debug.Assert(!invert);
            Debug.Assert(color.Name == "ff87ffd7");

            // grey
            (color, invert) = ColorFromAnsi("\033[38;5;249m");
            Debug.Assert(!invert);
            Debug.Assert(color.Name == "ffb2b2b2");

            // id bg
            (color, invert) = ColorFromAnsi("\033[48;5;231m");
            Debug.Assert(invert);
            Debug.Assert(color.Name == "ffffffff");


            //ESC[38;2;R;G;Bm
            // rgb
            (color, invert) = ColorFromAnsi("\033[38;2;204;39;187m");
            Debug.Assert(!invert);
            Debug.Assert(color.Name == "ffcc27bb");

            // rgb invert
            (color, invert) = ColorFromAnsi("\033[48;2;19;0;222m");
            Debug.Assert(invert);
            Debug.Assert(color.Name == "ff1300de");


            // Open file, process each line..
            /// Client searches for strings starting with "ESC[" and ending with "m".
            /// One of: ESC[IDm  ESC[38;5;IDm  ESC[48;5;IDm  ESC[38;2;R;G;Bm  ESC[48;2;R;G;Bm

            var lines = File.ReadAllLines(@"C:\Dev\repos\Apps\NTerm\Test\ross_color.txt");
            foreach (var l in lines)
            {
                int ind = l.IndexOf("\033[");
                if (ind >= 0)
                {

                }

            }

        }
    }


    //public class ANSI_1 : TestSuite
    //{
    //    public override void RunSuite()
    //    {
    //        int val1 = 1;

    //        UT_STOP_ON_FAIL(true);

    //        UT_INFO("Visually inspect that this appears in the output");

    //        UT_EQUAL(val1, 3);

    //        UT_INFO("Visually inspect that this does not appear in the output");
    //    }
    //}




}
