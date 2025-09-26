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

    /// <summary>Comm has something to tell the user.</summary>
    public class NotifEventArgs(Cat cat, string msg) : EventArgs
    {
        public Cat Cat { get; init; } = cat;
        public string Message { get; init; } = msg;
    }

    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Start the comm task.</summary>
        void Run(CancellationToken token);

        /// <summary>Send to the server.</summary>
        /// <param name="msg">What to send</param>
        void Send(byte[] msg);

        /// <summary>Receive from the server.</summary>
        /// <returns>Received message or null if none.</returns>
        byte[]? Receive();

        /// <summary>Reset comms, resource management.</summary>
        void Reset();

        /// <summary>I have something to say.</summary>
        event EventHandler<NotifEventArgs>? Notif;
    }
    #endregion
}
