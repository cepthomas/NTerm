using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    /// <summary>Default comm.</summary>
    /// <see cref="IComm"/>
    public class NullComm : IComm
    {
        #region Fields
        int _count = 0;
        #endregion

        public void Dispose()
        {
        }

        //public (OpStatus stat, string msg) Init(Config config)
        public NullComm(Config config)
        {
            //return (OpStatus.Success, $"NullComm inited at {DateTime.Now}");
        }

        #region IComm implementation
        //public Stream? AltStream { get; set; } = null;

        public (OpStatus stat, string msg) Send(string data)
        {
            return (OpStatus.Success, "Nothing to say");
        }

        public (OpStatus stat, string msg, string data) Receive()
        {
            // Fake blocking.
            Thread.Sleep(1000);
            _count += 11;
            return (OpStatus.Success, "no msg", $"NullComm receive {_count}");
        }

        public void Reset()
        {
        }        
        #endregion
    }
}
