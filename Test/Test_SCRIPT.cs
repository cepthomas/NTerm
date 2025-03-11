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
using Script;


namespace NTerm.Test
{
    public class SCRIPT_PASS : TestSuite
    {
        public override void RunSuite()
        {
            UT_INFO("Tests script happy functions.");

            // UT_TRUE(invert);
            // UT_EQUAL(color.Name, "ff7f007f");

            var thisDir = MiscUtils.GetSourcePath();

            var scriptFn = Path.Combine(thisDir, "simple-response.lua");

            using Interop scr = new();

            scr.Run(scriptFn, [thisDir]);

            for (int i = 0; i < 3; i++)
            {
                var rx = scr.Send($"tx:{(i * 2)}");
                UT_EQUAL(rx, "BOOGA");
            }
        }
    }

    public class SCRIPT_INTERRUPTS : TestSuite
    {
        public override void RunSuite()
        {
            UT_INFO("Tests script interrupt handling.");

            // Streams:::
            ///// Read(byte[] buffer,...) throw:
            // ArgumentNullException - The buffer passed is null.
            // InvalidOperationException - The specified port is not open.
            // ArgumentOutOfRangeException - The offset or count parameters are outside a valid region of the buffer being passed. Either offset or count is less than zero.
            // ArgumentException - offset plus count is greater than the length of the buffer.
            // TimeoutException - No bytes were available to read.
            ///// ReadByte():
            // InvalidOperationException - The specified port is not open.
            // TimeoutException - The operation did not complete before the time-out period ended.
            //    -or-
            // No byte was read.
            ///// Write(byte[] array, ...) :
            // InvalidOperationException - The specified port is not open.
            // ArgumentNullException - text is null.
            // TimeoutException - The operation did not complete before the time-out period ended.
            // or??
            // ArgumentException - The sum of offset and count is greater than the buffer length.
            // ArgumentNullException - buffer is null.
            // ArgumentOutOfRangeException - offset or count is negative.
            // IOException - An I/O error occurred, such as the specified file cannot be found.
            // NotSupportedException - The stream does not support writing.
            // ObjectDisposedException - Write(Byte[], Int32, Int32) was called after the stream was closed.
            ///// WriteByte() ::
            // IOException - An I/O error occurs.
            // NotSupportedException - The stream does not support writing, or the stream is already closed.
            // ObjectDisposedException - Methods were called after the stream was closed.

            // UT_TRUE(invert);
            // UT_EQUAL(color.Name, "ff7f007f");

            var thisDir = MiscUtils.GetSourcePath();

            var scriptFn = Path.Combine(thisDir, "simple-response.lua");

            using Interop scr = new();

            scr.Run(scriptFn, [thisDir]);


            for (int i = 0; i < 3; i++)
            {
                var rx = scr.Send($"tx:{(i * 2)}");
                UT_EQUAL(rx, "BOOGA");
            }
        }
    }
}
