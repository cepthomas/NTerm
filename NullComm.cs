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
        //int _count = 0;
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

        public void Init(List<string> config, IProgress<string> progress)
        {
            _progress = progress;
        }

        public void Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                List<string> res = [];

                if (_qsend.TryDequeue(out string? s))
                {
                    var sout = $">>>NullComm:{s}!{Environment.NewLine}";
                    // Simulate broken line.
                    res.Add(sout[..5]);
                    res.Add(sout[5..]);
                }

                res.ForEach(r => _progress.Report(r));


                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        void IComm.Send(string req)
        {
            for (int i = 0; i < 5; i++)
            {
                _qsend.Enqueue($"{req}*iter{i + 1}");
            }
        }

        ///// <summary>IComm implementation.</summary>
        ///// <see cref="IComm"/>
        //public void Reset()
        //{
        //}
    }
}
