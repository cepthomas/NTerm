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
using 

namespace NTerm
{


    /// <summary>A typical application.</summary>
    public class Host : IDisposable
    {
        #region Fields
        /// <summary>Script logger.</summary>
        readonly Logger _loggerScript = LogManager.CreateLogger("SCR");

        /// <summary>The interop.</summary>
        protected HostInterop _interop = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public Host()
        {
            // Where are we?
            var thisDir = MiscUtils.GetSourcePath();
            var lbotDir = Path.Combine(thisDir, get env $(LBOT)

            // Setup logging.
            LogManager.MinLevelFile = LogLevel.Trace;
            LogManager.MinLevelNotif = LogLevel.Info;
            LogManager.Run(Path.Combine(thisDir, "log.txt"), 50000);
            LogManager.LogMessage += (object? sender, LogMessageEventArgs e) => Console.WriteLine(e.ShortMessage);

            // TODOF Support reload. Unload current modules so reload will be minty fresh. This may fail safely

            try
            {
                // Hook script callbacks.
                HostInterop.Log += Interop_Log;
                //HostInterop.Notification += Interop_Notification;

                // Load script using specific lua script paths.
                var scriptFn = Path.Combine(thisDir, "script_test.lua");
                List<string> lpath = [thisDir, lbotDir];
                _interop.Run(scriptFn, lpath);

                // Execute script functions.
                int res = _interop.Setup(12345);
                for (int i = 0; i < res; i++)
                {
                    var cmdResp = _interop.DoCommand("cmd", (i*2).ToString());
                    _logger.Info($"cmd {i} gave me {cmdResp}");
                }
            }
            catch (InteropException ex)
            {
                _loggerScript.Exception(ex);
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }

            LogManager.Stop();
        }

        /// <summary>
        /// Clean up resources. https://stackoverflow.com/a/4935448
        /// </summary>
        public void Dispose()
        {
            _interop.Dispose();
        }
        #endregion

        #region Script Event Handlers
        /// <summary>
        /// Log something from script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_Log(object? sender, LogArgs args) 
        {
            var level = MathUtils.Constrain(args.level, (int)LogLevel.Trace, (int)LogLevel.Error);
            _loggerScript.Log((LogLevel)level, args.msg);
        }

        /// <summary>
        /// Log something from script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_Notification(object? sender, NotificationArgs args) 
        {
            _loggerScript.Info($"Notification: {args.num} {args.text}");
        }
        #endregion
    }


    // a test mock TODO1 implement
    class ScriptStream : Stream
    {
        // // What was last sent by the device.
        // public string WriteBuffer { get; private set; } = "";

        // // Set this to what the next read op gets.
        // public string ReadBuffer { get; private set; } = "";

        public void LoadScript(string fn)
        {

        }

        #region Stream implementation

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanTimeout => true;

        public override bool CanWrite => true;

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // TODO1 script indicates throw
// ArgumentNullException - The buffer passed is null.
// InvalidOperationException - The specified port is not open.
// ArgumentOutOfRangeException - The offset or count parameters are outside a valid region of the buffer being passed. Either offset or count is less than zero.
// ArgumentException - offset plus count is greater than the length of the buffer.
// TimeoutException - No bytes were available to read.

            int numRead = -1;

            //zero-based byte offset in buffer at which to begin storing the data
            //maximum number of bytes to be read from the current stream.

            // Check args.

            // Copy from ReadBuffer to buffer.


            return numRead;
        }
        public override int ReadByte()
        {
// InvalidOperationException - The specified port is not open.
// TimeoutException - The operation did not complete before the time-out period ended.
//    -or-
// No byte was read.

            // Reads a byte from the stream and advances the position within the stream by one byte,
            // or returns -1 if at the end of the stream.
            return 0;
        }

        public override void Write(byte[] array, int offset, int count)
        {
// InvalidOperationException - The specified port is not open.
// ArgumentNullException - text is null.
// TimeoutException - The operation did not complete before the time-out period ended.


            //write WriteBuffer

            // put something in ReadBuffer?
        }
        public override void WriteByte(byte value)
        {
        }

        public override void Flush()
        {
        }
        #endregion
    }






    /// <summary>Default comm.</summary>
    public class NullComm : IComm
    {
        #region IComm implementation
        public (OpStatus stat, string resp) Init(Config config)
        {
            return (OpStatus.Success, $"NullComm Inited at {DateTime.Now}{Environment.NewLine}");
        }

        public (OpStatus stat, string resp) Send(string? msg)
        {
            if (msg != null)
            {
                return (OpStatus.Success, $"NullComm got [{msg}] at {DateTime.Now}{Environment.NewLine}");
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
