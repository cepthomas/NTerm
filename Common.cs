using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    #region Types
    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Start the comm task.</summary>
        void Run(CancellationToken token);

        /// <summary>Send to the other end.</summary>
        /// <param name="msg">What to send</param>
        void Send(byte[] msg);

        /// <summary>Receive from the other end.</summary>
        /// <returns>Received message/error or null if none.</returns>
        object? GetReceive();

        /// <summary>Reset comms, resource management.</summary>
        void Reset();
    }

    #endregion
    /// <summary>Comm error processing categories.</summary>
    public enum CommState
    {
        Ok,          // Keep going
        Timeout,     // Try again later - forever
        Recoverable, // Normal bump e.g. server down, power - retry (with limit?)
        Fatal,       // Config error, hard runtime error, it's dead Jim
    }

    /// <summary>Comm error processing categories.</summary>
    public class Defs // Control or CChar or ??
    {
        // Some control characters.
        public const int LF = 10;
        public const int CR = 13;
        public const int NUL = 0;
        public const int ESC = 27;
    }
}
