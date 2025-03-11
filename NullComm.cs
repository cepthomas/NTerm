using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    /// <summary>Default comm.</summary>
    public class NullComm : IComm
    {
        #region IComm implementation
        public (OpStatus stat, string rx) Init(Config config)
        {
            return (OpStatus.Success, $"NullComm Inited at {DateTime.Now}{Environment.NewLine}");
        }

        public (OpStatus stat, string rx) Send(string? tx)
        {
            if (tx != null)
            {
                return (OpStatus.Success, $"NullComm sent [{tx ?? "null"}] at {DateTime.Now}{Environment.NewLine}");
            }
            else
            {
                return (OpStatus.NoResp, "");
            }
        }

        public void Dispose() { }
        #endregion
    }
}
