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


namespace NTermTest
{
    public class CommEventArgs : EventArgs
    {
        public byte[] TxBuff { get; set; } = new byte[0];
        public byte[] RxBuff { get; set; } = new byte[0];
    }



    /// <summary>Script flavor of stream.</summary>
    /// <see cref="Stream"/>
    public class MockStream : Stream
    {
        /// <summary>Throw this exception on next call.</summary>
        public Exception? ThrowMe { get; set; } = null;

        public event EventHandler<CommEventArgs>? CommEvent;

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

            // Ask the host.
            CommEvent?.Invoke(null, new());// { Category = cat, Message = msg });//

            //int toCopy = Math.Min(count, rx.Length);
            // Check args.

            //zero-based byte offset in buffer at which to begin storing the data
            //maximum number of bytes to be read from the current stream.

            //int i;
            //for (i = 0; i < toCopy && i < buffer.Length; i++)
            //{
            //    buffer[offset + i] = (byte)rx[i];
            //}

            return 0;// i;
        }

        public override int ReadByte()
        {
            MaybeThrow();
            // Check args.

            // Reads a byte from the stream and advances the position within the stream by one byte,
            // or returns -1 if at the end of the stream.

            // Ask the script.
            var rx = new byte[0];// _script.Send($"R1");
            
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

            var str = Utils.BytesToString(buff);
            var rx = new byte[0];// _script.Send(str);
        }

        public override void WriteByte(byte value)
        {
            MaybeThrow();

            var s = ((char)value).ToString();
            var rx = new byte[0];
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
