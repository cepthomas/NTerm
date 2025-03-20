using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    /// <summary>Default comm.</summary>
    /// <see cref="IComm"/>
    public class NullComm : IComm
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("NUL");
        //protected Interop _script = new();
        Config _config = new();
        #endregion

        #region IComm implementation
        public Stream? AltStream { get; set; } = null;

        public (OpStatus stat, string msg) Init(Config config)
        {
            _config = config;
            string msg = $"NullComm inited at {DateTime.Now}{Environment.NewLine}";
            return (OpStatus.Success, $"NullComm inited at {DateTime.Now}{Environment.NewLine}");
        }

        //public (OpStatus stat, string rx) Send(string? tx)
        //{
        //    return (OpStatus.Success, $"NullComm sent [{tx ?? "null"}] at {DateTime.Now}{Environment.NewLine}");
        //}

        public (OpStatus stat, byte[] rx) Send(byte[] tx)
        {
            var stx = Utils.BytesToString(tx);
            var srx = Utils.StringToBytes($"NullComm sent [{stx}] at {DateTime.Now}{Environment.NewLine}");
            return (OpStatus.Success, srx);
        }

        public void Dispose()
        {
            AltStream?.Dispose();
            AltStream = null;
        }
        #endregion
    }
}
