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
        int _count = 0;
        readonly ConcurrentQueue<string> _qsend = new();
        IProgress<string> _progress;
        #endregion


        // /// <summary>
        // /// Make me one.
        // /// </summary>
        // /// <param name="config"></param>
        // public NullComm()
        // {
        // }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
        }

        public OpStatus Init(List<string> config, IProgress<string> progress)
        {
            _progress = progress;

            return OpStatus.Success;//, $"NullComm inited at {DateTime.Now}");
        }

        public void Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_qsend.TryDequeue(out string? s))
                {
                    _progress.Report($">>> Got:{s}");
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        OpStatus IComm.Send(string req)
        {
            OpStatus stat = OpStatus.Success;
            _qsend.Enqueue(req);
            return stat;
        }

        ///// <summary>IComm implementation.</summary>
        ///// <see cref="IComm"/>
        //public void Reset()
        //{
        //}
    }
}
