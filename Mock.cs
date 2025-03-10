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

    // a test mock TODO1 implement
    class ScriptStream : Stream
    {
        // // What was last sent by the device.
        // public string WriteBuffer { get; private set; } = "";

        // // Set this to what the next read op gets.
        // public string ReadBuffer { get; private set; } = "";

        protected Script _script;

        public ScriptStream(string scriptFn, List<string> luaPaths)
        {
            _script = new(scriptFn, luaPaths);
        }


        public bool Throw { get; set; }

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

        public override int Read(byte[] buffer, int offset, int count) // TODO1
        {
            if (Throw)
            {

            }
            // TODO1 script indicates throw:
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
        public override int ReadByte()// TODO1
        {
            if (Throw)
            {
                // InvalidOperationException - The specified port is not open.
                // TimeoutException - The operation did not complete before the time-out period ended.
                //    -or-
                // No byte was read.
            }

            // Reads a byte from the stream and advances the position within the stream by one byte,
            // or returns -1 if at the end of the stream.
            
            return 0;
        }

        public override void Write(byte[] array, int offset, int count)// TODO1
        {
            if (Throw)
            {
                // InvalidOperationException - The specified port is not open.
                // ArgumentNullException - text is null.
                // TimeoutException - The operation did not complete before the time-out period ended.

                // ArgumentException - The sum of offset and count is greater than the buffer length.
                // ArgumentNullException - buffer is null.
                // ArgumentOutOfRangeException - offset or count is negative.
                // IOException - An I/O error occurred, such as the specified file cannot be found.
                // NotSupportedException - The stream does not support writing.
                // ObjectDisposedException - Write(Byte[], Int32, Int32) was called after the stream was closed.
            }


            //write WriteBuffer

            // put something in ReadBuffer?
        }
        public override void WriteByte(byte value)// TODO1
        {
            if (Throw)
            {
                // IOException - An I/O error occurs.
                // NotSupportedException - The stream does not support writing, or the stream is already closed.
                // ObjectDisposedException - Methods were called after the stream was closed.
            }


        }

        public override void Flush()
        {
        }
        #endregion
    }



    /// <summary>Script flavor comm.</summary>
    public class ScriptComm : IComm
    {
        Config _config;
        protected Script _script;

        public ScriptComm(string scriptFn, List<string> luaPaths)
        {
            _script = new(scriptFn, luaPaths);
        }


        #region IComm implementation
        public (OpStatus stat, string resp) Init(Config config)
        {
            _config = config;
            return (OpStatus.Success, $"ScriptComm Inited at {DateTime.Now}{Environment.NewLine}");
        }

        public (OpStatus stat, string resp) Send(string? msg)
        {
            if (msg != null)
            {
                return (OpStatus.Success, _script.Send(msg));
            }
            else
            {
                return (OpStatus.NoResp, "");
            }
        }

        public void Dispose() { }
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
