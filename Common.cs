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
    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Start the comm task.</summary>
        void Run(CancellationToken token);

        /// <summary>Send to the other end.</summary>
        /// <param name="msg">What to send</param>
        void Send(byte[] msg);

        /// <summary>Receive from the other end.</summary>
        /// <returns>Received message or null if none.</returns>
        byte[]? Receive();

        /// <summary>Reset comms, resource management.</summary>
        void Reset();

        /// <summary>I have something to say.</summary>
        event EventHandler<NotifEventArgs>? Notif;
    }

    /// <summary>Print/log categories.</summary>
    public enum Cat
    {
        Log,        // to log
        Info,       // to user
        Error,      // to user and log
        Send,       // to user and log
        Receive,    // to user and log
    }

    /// <summary>Comm has something to tell the user.</summary>
    public class NotifEventArgs(Cat cat, string msg) : EventArgs
    {
        public Cat Cat { get; init; } = cat;
        public string Message { get; init; } = msg;
    }

    #endregion
}
