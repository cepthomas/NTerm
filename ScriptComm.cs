using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    /// <summary>Default comm.</summary>
    public class ScriptComm : IComm
    {
        #region IComm implementation
// ArgumentException - The sum of offset and count is larger than the buffer length.
// ArgumentNullException - buffer is null.
// ArgumentOutOfRangeException - offset or count is negative.
// IOException - An I/O error occurs.
// NotSupportedException - The stream does not support reading.
// ObjectDisposedException - Methods were called after the stream was closed.

        public (OpStatus stat, string resp) Init(Config config) // TODO1 - open scr file
        {
            return (OpStatus.Success, "");
        }

        public (OpStatus stat, string resp) Send(string? msg) // TODO1 - WR/RD scr
        {
            var resp = $"ScriptComm send [{msg}] at {DateTime.Now}";
            return (OpStatus.Success, resp);
        }

        public void Dispose() { }
        #endregion
    }
}
