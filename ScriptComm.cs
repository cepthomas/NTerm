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
using Script;

namespace NTerm
{
    /// <summary>Script flavor of IComm.</summary>
    /// <see cref="IComm"/>
    public class ScriptComm : IComm
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("SCR");
        protected Interop _script = new();
        Config _config = new();
        #endregion


        #region IComm implementation
        public (OpStatus stat, string msg) Init(Config config)
        {
            _config = config;
            string scriptFn = _config.Args;
            List<string> luaPaths = [];

            Interop.Log += (object? sender, LogArgs args) => _logger.Log(args.msg ? LogLevel.Error : LogLevel.Info, args.msg);
            _script.Run(scriptFn, luaPaths);

            OpStatus stat = OpStatus.Success;
            string msg = $"ScriptComm inited at {DateTime.Now}{Environment.NewLine}";
            return (stat, msg);
        }

        public (OpStatus stat, string rx) Send(string? tx)
        {
            // Execute script functions.
            var rx = _script.Send(tx ?? "P");
            _logger.Info($"tx:{tx} rx:{rx}");

            return (OpStatus.Success, rx); // OpStatus.NoResp?
        }

        public void Dispose()
        {
            _script.Dispose();
        }

        #endregion
    }
}