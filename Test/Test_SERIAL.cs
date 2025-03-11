using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using NTerm;


namespace NTerm.Test
{
    public class SERIAL_COMM : TestSuite
    {
        public override void RunSuite()
        {
            UT_INFO("Tests serial port functions.");

            // UT_TRUE(invert);
            // UT_EQUAL(color.Name, "ff7f007f");


        }
    }




    // a test serial mock
    public class SerialStreamEmu : Stream//TOODO1
    {
        long _length = 0;
        long _position = 0;

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
            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return 0;
        }

        public override void Write(byte[] array, int offset, int count)
        {
            //Write(array, offset, count, WriteTimeout);
        }

        public override void Flush()
        {
        }


        ~SerialStreamEmu()
        {
            Dispose(false);
        }

        /////// debug stuff
        public string WriteBuffer { get; private set; } = "";
        public string ReadBuffer { get; private set; } = "";
    }


    public class SerialPortEmu //: ISerialPort
    {
        SerialStreamEmu _stream = new();

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
