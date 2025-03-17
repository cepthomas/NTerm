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
    /// <summary>Script flavor of stream.</summary>
    /// <see cref="Stream"/>
    public class ScriptStream : Stream
    {

        //public (OpStatus stat, string msg) Init(Config config)
        //{
        //    _config = config;

        //    if (_config.Args.Count != 2)
        //    {
        //        throw new ArgumentException($"Invalid config arguments");
        //    }
        //    string scriptFn = _config.Args[0];
        //    string luaPath = _config.Args[1];

        //    Interop.Log += (object? sender, LogArgs args) => _logger.Log(args.err ? LogLevel.Error : LogLevel.Info, args.msg);
        //    _script.Run(scriptFn, luaPath);

        //    OpStatus stat = OpStatus.Success;
        //    string msg = $"ScriptComm inited at {DateTime.Now}{Environment.NewLine}";
        //    return (stat, msg);
        //}

        //public (OpStatus stat, string rx) Send(string? tx)
        //{
        //    // Execute script functions.
        //    var rx = _script.Send(tx ?? "P");
        //    _logger.Info($"tx:{tx} rx:{rx}");

        //    return (OpStatus.Success, rx); // OpStatus.NoResp?
        //}







        /// <summary>The script object.</summary>
        readonly Interop _script = new();

        /// <summary>Throw this exception on next call.</summary>
        public Exception? ThrowMe { get; set; } = null;

        /// <summary>Constructor.</summary>
        /// <param name="scriptFn"></param>
        /// <param name="luaPath"></param>
        public ScriptStream(string scriptFn, string luaPath)
        {
            _script.Run(scriptFn, luaPath);
        }

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

            // Ask the script.
            var rx = _script.Send($"R{count}");

            int toCopy = Math.Min(count, rx.Length);
            // Check args.

            //zero-based byte offset in buffer at which to begin storing the data
            //maximum number of bytes to be read from the current stream.

            int i;
            for (i = 0; i < toCopy && i < buffer.Length; i++)
            {
                buffer[offset + i] = (byte)rx[i];
            }

            return i;
        }

        public override int ReadByte()
        {
            MaybeThrow();
            // Check args.

            // Reads a byte from the stream and advances the position within the stream by one byte,
            // or returns -1 if at the end of the stream.

            // Ask the script.
            var rx = _script.Send($"R1");
            
            return rx.Length == 0 ? -1 : rx[0];
        }

        public override void Write(byte[] array, int offset, int count)
        {
            MaybeThrow();
            // Check args.

            // Copy from array starting at offset for count.
            int toSend = Math.Min(count, array.Length - offset);
            byte[] buff = new byte[toSend]; 
            
            int i;
            for (i = 0; i < toSend && i < array.Length - offset; i++)
            {
                buff[i] = array[offset + i];
            }

            var str = Encoding.Default.GetString(buff);
            var rx = _script.Send(str);
        }

        public override void WriteByte(byte value)
        {
            MaybeThrow();

            var s = ((char)value).ToString();
            var rx = _script.Send(s);
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
