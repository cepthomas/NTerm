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
    public class DebugComm : IComm
    {
        #region IComm implementation
        public int ResponseTime { get; set; } = 500;
        public int BufferSize { get; set; } = 4096;
        public string Response { get; private set; } = "";

// ArgumentException - The sum of offset and count is larger than the buffer length.
// ArgumentNullException - buffer is null.
// ArgumentOutOfRangeException - offset or count is negative.
// IOException - An I/O error occurs.
// NotSupportedException - The stream does not support reading.
// ObjectDisposedException - Methods were called after the stream was closed.

        public OpStatus Init(string args)
        {
            return OpStatus.Success;
        }

        public OpStatus Send(string msg)
        {
            Response = $"TODO DebugComm send [{msg}] at {DateTime.Now}"; return OpStatus.Success;
        }

        public void Dispose()
        {
        }
        #endregion
    }
}
