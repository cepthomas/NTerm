using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    /// <summary>Supported flavors  .</summary>
    public enum CommType { Null, Tcp, Serial }

    /// <summary>How did we do?</summary>
    public enum OpStatus { Success, Timeout, Error, ConfigError }

    /// <summary>Comm type implementation.</summary>
    public interface IComm : IDisposable
    {
        /// <summary>Server must connect or reply to commands in msec.</summary>
        int ResponseTime { get; set; }

        /// <summary>R/W buffer size.</summary>
        int BufferSize { get; set; }

        /// <summary>The text if Success otherwise error message.</summary>
        string Response { get; }

        /// <summary>Send a message to the server.</summary>
        /// <param name="msg">What to send.</param>
        /// <returns>Operation status.</returns>
        OpStatus Init(string args);

        /// <summary>Send a message to the server.</summary>
        /// <param name="msg">What to send.</param>
        /// <returns>Operation status.</returns>
        OpStatus Send(string msg);

        /// <summary>Clean up.</summary>
        public new void Dispose();
    }

    /// <summary>Default comm.</summary>
    public class NullComm : IComm
    {
        #region IComm implementation
        public int ResponseTime { get; set; } = 500;
        public int BufferSize { get; set; } = 4096;
        public string Response { get; private set; } = "Nothing to see here";
        public OpStatus Init(string args) { return OpStatus.Success; }
        public OpStatus Send(string msg) { Response = $"You sent me [{msg}] at {DateTime.Now}"; return OpStatus.Success; }
        public void Dispose() { }
        #endregion
    }
}
