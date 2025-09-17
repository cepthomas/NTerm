using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    /// <summary>Default comm.</summary>
    /// <see cref="IComm"/>
    public class NullComm : IComm
    {
        #region Fields
        readonly ConcurrentQueue<string> _qSend = new();
        readonly ConcurrentQueue<byte[]> _qRecv = new();
        #endregion

        #region Lifecycle
        /// <summary>Constructor.</summary>
        public NullComm()
        {
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return ($"NullComm");
        }
        #endregion

        #region IComm implementation
        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(string req)
        {
            _qSend.Enqueue(req + '\n');
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public byte[]? Receive()
        {
            _qRecv.TryDequeue(out byte[]? res);
            return res;
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Run(CancellationToken token)
        {
            Random rand = new();

            while (!token.IsCancellationRequested)
            {
                if (_qSend.TryDequeue(out string? s))
                {
                    // Loopback. Could modify it also.
                    _qRecv.Enqueue(Encoding.Default.GetBytes(s));
                }

                // Don't be greedy.
                Thread.Sleep(5);
            }
        }
        #endregion
    }
}
