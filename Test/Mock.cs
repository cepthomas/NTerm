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
//using Script;


namespace NTerm
{

    /// <summary>Script flavor of comm.</summary>
    public class ScriptComm : IComm
    {
        #region Fields
        /// <summary>Script logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("SCR");

        /// <summary>The script object.</summary>
        protected Script.Interop _script = new();

        /// <summary>The configuration.</summary>
        Config _config;
        #endregion


        public ScriptComm(string scriptFn, List<string> luaPaths)
        {
            Script.Interop.Log += (object? sender, LogArgs args) => _logger.Log(args.err ? LogLevel.Error : LogLevel.Info, args.msg);
            _script.Run(scriptFn, luaPaths);
        }


        #region IComm implementation
        public (OpStatus stat, string rx) Init(Config config)
        {
            _config = config;
            return (OpStatus.Success, $"Mock Script inited at {DateTime.Now}{Environment.NewLine}");
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



    /// <summary>Script flavor of stream.</summary>
    public class ScriptStream : Stream
    {
        // // What was last sent by the device.
        // public string WriteBuffer { get; private set; } = "";

        // // Set this to what the next read op gets.
        // public string ReadBuffer { get; private set; } = "";

        /// <summary>The script object.</summary>
        protected Script.Interop _script = new();

        public ScriptStream(string scriptFn, List<string> luaPaths)
        {
            _script.Run(scriptFn, luaPaths);
        }


        /// <summary>Throw this exception on next call.</summary>
        public Exception? ThrowMe { get; set; } = null;

///// Read(byte[] buffer,...) throw:
// ArgumentNullException - The buffer passed is null.
// InvalidOperationException - The specified port is not open.
// ArgumentOutOfRangeException - The offset or count parameters are outside a valid region of the buffer being passed. Either offset or count is less than zero.
// ArgumentException - offset plus count is greater than the length of the buffer.
// TimeoutException - No bytes were available to read.
///// ReadByte():
// InvalidOperationException - The specified port is not open.
// TimeoutException - The operation did not complete before the time-out period ended.
//    -or-
// No byte was read.
///// Write(byte[] array, ...) :
// InvalidOperationException - The specified port is not open.
// ArgumentNullException - text is null.
// TimeoutException - The operation did not complete before the time-out period ended.
// or??
// ArgumentException - The sum of offset and count is greater than the buffer length.
// ArgumentNullException - buffer is null.
// ArgumentOutOfRangeException - offset or count is negative.
// IOException - An I/O error occurred, such as the specified file cannot be found.
// NotSupportedException - The stream does not support writing.
// ObjectDisposedException - Write(Byte[], Int32, Int32) was called after the stream was closed.
///// WriteByte() ::
// IOException - An I/O error occurs.
// NotSupportedException - The stream does not support writing, or the stream is already closed.
// ObjectDisposedException - Methods were called after the stream was closed.


        #region Stream implementation

        #region Stubs
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

        public override void Flush() { }
        #endregion

        #region Real work

        public override int Read(byte[] buffer, int offset, int count)
        {
            MaybeThrow();

            //zero-based byte offset in buffer at which to begin storing the data
            //maximum number of bytes to be read from the current stream.

            // Check args.

            int numRead = -1;

            // Ask the script.
            var rx = _script.Send($"R{count}");
            _logger.Info($"rx:{rx}");

        //=>    copy rx[0..count] to buffer[offset]

            return numRead;
        }

        public override int ReadByte()
        {
            MaybeThrow();

            // Reads a byte from the stream and advances the position within the stream by one byte,
            // or returns -1 if at the end of the stream.

            // Check args.

            // Ask the script.
            var rx = _script.Send($"R1");
            _logger.Info($"rx:{rx}");

            
            return rx.Length == 0 ? -1 : rx[0];
        }

        public override void Write(byte[] array, int offset, int count)
        {
            MaybeThrow();

            // Check args.
            // Copy from array starting at offset for count.
            var tx = byte[];

            var rx = _script.Send(tx);
            _logger.Info($"rx:{rx} tx:{tx}");

        }

        public override void WriteByte(byte value)
        {
            MaybeThrow();

            var rx = _script.Send([value]);
            _logger.Info($"rx:{rx} tx:{tx}");
        }
        #endregion


        #endregion

        #region Internals

        void MaybeThrow()
        {
            if (ThrowMe is not null)
            {
                var ex = ThrowMe;
                ThrowMe = null;
                throw ex;
            }
        }
        #endregion

    }
}
