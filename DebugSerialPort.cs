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
    // a test serial mock
    public class DebugSerialStream : Stream
    {
        long _length = 0;
        long _position = 0;


        // What was last sent by the device.
        public string WriteBuffer { get; private set; } = "";

        // Set this to what the next read op gets.
        public string ReadBuffer { get; private set; } = "";

        #region Stream implementation
// ArgumentException - The sum of offset and count is larger than the buffer length.
// ArgumentNullException - buffer is null.
// ArgumentOutOfRangeException - offset or count is negative.
// IOException - An I/O error occurs.
// NotSupportedException - The stream does not support reading.
// ObjectDisposedException - Methods were called after the stream was closed.

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanTimeout => true;

        public override bool CanWrite => true;

        public override long Length { get { return _length; } }

        public override long Position
        {
            get { return _position; }
            set { _position = value; }
        }





        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
        }

        public override int ReadByte()
        {
            //Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
            return 0;
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            int numRead = -1;

            //zero-based byte offset in buffer at which to begin storing the data
            //maximum number of bytes to be read from the current stream.

            // Check args.

            // Copy from ReadBuffer to buffer.


            return numRead;
        }

        public override void Write(byte[] array, int offset, int count)
        {

            //write WriteBuffer

            // put something in ReadBuffer?
        }

        public override void Flush()
        {
        }


        // ~DebugSerialStream()
        // {
        //     Dispose(false);
        // }
        #endregion
    }


    public class DebugSerialPort : ISerialPort
    {
        DebugSerialStream _stream = new();

        #region ISerialPort implementation
        public int ReadBufferSize { get; set; }
        public int WriteBufferSize { get; set; }
        public int ReadTimeout { get; set; }
        public int WriteTimeout { get; set; }
        public string PortName { get; set; }
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
}
