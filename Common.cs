using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    #region Types
    /// <summary>General categories, mainly for logging.</summary>
    public enum Cat { None, Send, Receive, Error, Info }

    /// <summary>Reporting user errors.</summary>
    public class ConfigException(string message) : Exception(message);

    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Start the comm task.</summary>
        void Run(CancellationToken token);

        /// <summary>Send to the server.</summary>
        /// <param name="msg">What to send</param>
        void Send(string msg);

        /// <summary>Receive from the server.</summary>
        /// <returns>Received message or null if none.</returns>
        byte[]? Receive();

        /// <summary>Reset comms, resource management.</summary>
        void Reset();
    }
    #endregion
}
