using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    /// <summary>Default comm.</summary>
    /// <see cref="IComm"/>
    public class NullComm : IComm
    {
        #region Fields
        readonly ConcurrentQueue<string> _qsend = new();
        readonly ConcurrentQueue<string> _qrecv = new();
        #endregion

        /// <summary>Constructor.</summary>
        public NullComm()
        {
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Run(CancellationToken token)
        {
            Random rand = new();

            while (!token.IsCancellationRequested)
            {
                List<string> res = [];

                if (_qsend.TryDequeue(out string? s))
                {
                    var sout = $">>>NullComm:{s}!{Environment.NewLine}";
                    // Simulate broken lines.
                    int lb = rand.Next(1, sout.Length - 2);
                    _qrecv.Enqueue(sout[..lb]);
                    _qrecv.Enqueue(sout[lb..]);
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(string req)
        {
            for (int i = 0; i < 5; i++)
            {
                _qsend.Enqueue($"{req}*iter{i + 1}");
            }
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public string? Receive()
        {
            _qrecv.TryDequeue(out string? res);
            return res;
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }
    }
}
