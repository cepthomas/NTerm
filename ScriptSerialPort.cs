using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    // mock serial port
    public class ScriptSerialPort : ISerialPort
    {

        ScriptSerialStream _stream = new();

        #region ISerialPort implementation
        public int ReadBufferSize { get; set; }
        public int WriteBufferSize { get; set; }
        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }
        public string PortName { get; set; } = "???";
        public int BaudRate { get; set; }
        public Parity Parity { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }
        public bool IsOpen { get; private set; }
        public Stream BaseStream { get { return _stream; } }

        public void Close()
        {
            IsOpen = false;
        }

        public void Dispose()
        {
            Close();
        }

        public void Open()
        {
            IsOpen = true;
        }
        #endregion
    }

    // a test serial mock
    class ScriptSerialStream : Stream
    {
        // // What was last sent by the device.
        // public string WriteBuffer { get; private set; } = "";

        // // Set this to what the next read op gets.
        // public string ReadBuffer { get; private set; } = "";

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

        public override int Read(byte[] buffer, int offset, int count)// TODO1 scrip RD
        {
            // TODO1 script throw
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
// -or-
// No byte was read.

            // Reads a byte from the stream and advances the position within the stream by one byte,
            // or returns -1 if at the end of the stream.
            return 0;
        }

        public override void Write(byte[] array, int offset, int count) // TODO1 script WR
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

// ArgumentException - The sum of offset and count is larger than the buffer length.
// ArgumentNullException - buffer is null.
// ArgumentOutOfRangeException - offset or count is negative.
// IOException - An I/O error occurs.
// NotSupportedException - The stream does not support reading.
// ObjectDisposedException - Methods were called after the stream was closed.

}
