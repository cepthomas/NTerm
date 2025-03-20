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


namespace NTermTest
{
    public class SERIAL_COMM : TestSuite // TODOF
    {
        public override void RunSuite()
        {
            UT_INFO("Tests serial port functions.");

            // Use ScriptStream as mock.

            MockStream ms = new();


            var i = 99;
            UT_TRUE(i == 99);
            UT_EQUAL(i, 101);


        }
    }
}
