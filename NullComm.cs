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
        readonly ConcurrentQueue<byte[]> _qSend = new();
        readonly ConcurrentQueue<byte[]> _qRecv = new();
        #endregion

        /// <summary>Module logger.</summary>
        //readonly Logger _logger = LogManager.CreateLogger("NUL");

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
        public CommState State { get; private set; }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(byte[] req)
        {
            _qSend.Enqueue(req);
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

        ///// <summary>Main work loop.</summary>
        ///// <see cref="IComm"/>
        //public event EventHandler<NotifEventArgs>? Notif;
        #endregion

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Run(CancellationToken token)
        {
            //Notif?.Invoke(this, new(Cat.Log, "xyzzy"));
            //_logger.Info("Run start");

            while (!token.IsCancellationRequested)
            {
                if (_qSend.TryDequeue(out byte[]? rd))
                {
                    // Loopback. Can modify it for test.
                    _qRecv.Enqueue(Encoding.Default.GetBytes("LP:"));
                    _qRecv.Enqueue(rd);
                }

                // Don't be greedy.
                Thread.Sleep(5);
            }
        }
    }
}
