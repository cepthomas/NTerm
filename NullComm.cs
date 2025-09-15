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
    public class NullComm : IComm //TODO1 put in test and make true null modem.
    {
        #region Fields
        readonly ConcurrentQueue<string> _qsend = new();
        readonly ConcurrentQueue<byte[]> _qrecv = new();
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
        public void Send(string req)
        {
            for (int i = 0; i < 5; i++)
            {
                _qsend.Enqueue($"{req}*iter{i + 1}");
            }
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public byte[]? Receive()
        {
            _qrecv.TryDequeue(out byte[]? res);
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
                if (_qsend.TryDequeue(out string? s))
                {
                    var sout = $"NullComm-recv:{s}|\n";
                    // Simulate broken lines.
                    int lb = rand.Next(1, sout.Length - 2);
                    _qrecv.Enqueue(Encoding.Default.GetBytes(sout[..lb]));
                    _qrecv.Enqueue(Encoding.Default.GetBytes(sout[lb..]));
                }

                // Don't be greedy.
                Thread.Sleep(5);
            }
        }
    }
}
