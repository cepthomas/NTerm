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
        readonly ConcurrentQueue<object> _qRecv = new();
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
            return $"NullComm";
        }
        #endregion

        #region IComm implementation
        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(byte[] req)
        {
            _qRecv.Enqueue($"++++[{req}]");
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public object? GetReceive()
        {
            _qRecv.TryDequeue(out object? res);
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
            while (!token.IsCancellationRequested)
            {
                // Don't be greedy.
                Thread.Sleep(50);
            }
        }
        #endregion
    }
}
