using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    /// <summary>Default comm.</summary>
    /// <see cref="IComm"/>
    public class NullComm : IComm
    {
        #region Fields
        readonly ConcurrentQueue<byte[]> _qSend = new();
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

        /// <summary>Send help.</summary>
        public static List<string> Usage()
        {
            return ["null"];
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return $"NullComm";
        }
        #endregion

        #region IComm implementation
        /// <see cref="IComm"/>
        public void Send(byte[] td)
        {
            _qSend.Enqueue(td);
        }

        /// <see cref="IComm"/>
        public void Reset()
        {
        }

        /// <see cref="IComm"/>
        public async Task Run(CancellationToken token, IProgress<byte[]> progress)
        {
            bool done = false;

            while (!done)
            {
                token.ThrowIfCancellationRequested();

                if (_qSend.TryDequeue(out byte[]? td))
                {
                    Array.Reverse(td);
                    progress.Report(td);
                }

                // Don't be greedy.
                await Task.Delay(50, token);
            }
        }
        #endregion
    }
}
