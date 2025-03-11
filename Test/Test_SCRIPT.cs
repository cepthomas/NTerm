using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using NTerm;


namespace NTerm.Test
{
    public class SCRIPT_PASS : TestSuite //TODO1
    {
        public override void RunSuite()
        {
            UT_INFO("Tests script happy functions.");

            // UT_TRUE(invert);
            // UT_EQUAL(color.Name, "ff7f007f");

            var thisDir = MiscUtils.GetSourcePath();

            var scriptFn = Path.Combine(thisDir, "response-script.lua");

            using Script scr = new(scriptFn, [thisDir]);


            for (int i = 0; i < res; i++)
            {
                var rx = scr.Send($"tx:{(i*2)}");
                UT_STR_EQUAL(rx, "BOOGA");
            }
        }
    }
}
