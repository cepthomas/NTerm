using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    /// <summary>Default comm.</summary>
    /// <see cref="IComm"/>
    public class NullComm : IComm
    {
        #region IComm implementation
        public (OpStatus stat, string err) Init(Config config)
        {
            return (OpStatus.Success, $"NullComm Inited at {DateTime.Now}{Environment.NewLine}");
        }

        public (OpStatus stat, string rx) Send(string? tx)
        {
            return (OpStatus.Success, $"NullComm sent [{tx ?? "null"}] at {DateTime.Now}{Environment.NewLine}");
        }

        public void Dispose() { }
        #endregion
    }
}
