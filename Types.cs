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
    /// <summary>Supported flavors.</summary>
    public enum CommType
    {
        None,       // aka Debug
        Tcp,
        Serial
    }

    /// <summary>Supported flavors.</summary>
    public enum CommMode
    {
        CmdResp,    // each command expects a response
        Poll        // check periodically for server msgs
    }

    /// <summary>How did operation turn out?</summary>
    public enum OpStatus
    {
        Success,    // okeydokey
        Timeout,    // try again
        NoResp,     // poll no answer
        Error       // it's dead jim
    }

    /// <summary></summary>
    public enum ColorMode
    {
        None,
        Ansi,
        Match
    }

    /// <summary>Internal version of core types.</summary>
    enum Modifier
    {
        None,
        Ctrl,
        Alt
    }

    /// <summary>Internal data container.</summary>
    record CliInput(Modifier Mod, string Text);

    /// <summary>Spec for one match.</summary>
    /// <param name="Text"></param>
    /// <param name="WholeWord"></param>
    /// <param name="WholeLine"></param>
    /// <param name="ForeColor"></param>
    /// <param name="BackColor"></param>
    public record Matcher(string Text, bool WholeWord, bool WholeLine, Color? ForeColor, Color? BackColor);

    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Initialize the comm device.</summary>
        /// <param name="config">Setup info.</param>
        /// <returns>Operation status, response.</returns>
        (OpStatus stat, string resp) Init(Config config);

        /// <summary>Send a message to the server.</summary>
        /// <param name="msg">What to send. Null indicates a poll request.</param>
        /// <returns>Operation status, response.</returns>
        (OpStatus stat, string resp) Send(string? msg);

        /// <summary>Clean up.</summary>
        public new void Dispose();
    }
}
