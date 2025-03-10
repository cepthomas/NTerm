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
using ScriptInterop;


namespace Script
{
    /// <summary></summary>
    public class Script : IDisposable
    {
        #region Fields
        /// <summary>Script logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("SCR");

        /// <summary>The interop.</summary>
        protected ScriptInterop _interop = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// TODOF Support reload. Unload current modules so reload will be minty fresh. This may fail safely
        /// </summary>
        public Script(string scriptFn, List<string> luaPaths)
        {
            try
            {
                Interop.Log += (object? sender, LogArgs args) => _logger.Log((LogLevel)level, args.msg);

                // Load script using specific lua script paths.
                _interop.Run(scriptFn, luaPaths);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
        }

        public string Send(string msg)
        {
            // Execute script functions.
            var resp = _interop.DoCommand("cmd", (i*2).ToString());
            _logger.Info($"cmd {i} gave me {resp}");
            return resp;
        }

        /// <summary>
        /// Clean up resources. https://stackoverflow.com/a/4935448
        /// </summary>
        public void Dispose()
        {
            _interop.Dispose();
        }
        #endregion
    }
}
