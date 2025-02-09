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


namespace NTerm
{
    /// <summary>Supported flavors  .</summary>
    public enum CommType { Null, Tcp, Serial }

    /// <summary>How did we do?</summary>
    public enum OpStatus { Success, Timeout, Error, ConfigError }

    /// <summary>Comm type implementation.</summary>
    public interface IComm : IDisposable
    {
        /// <summary>Server must connect or reply to commands in msec.</summary>
        int ResponseTime { get; set; }

        /// <summary>R/W buffer size.</summary>
        int BufferSize { get; set; }

        /// <summary>The text if Success otherwise error message.</summary>
        string Response { get; }

        /// <summary>Send a message to the server.</summary>
        /// <param name="msg">What to send.</param>
        /// <returns>Operation status.</returns>
        OpStatus Init(string args);

        /// <summary>Send a message to the server.</summary>
        /// <param name="msg">What to send.</param>
        /// <returns>Operation status.</returns>
        OpStatus Send(string msg);

        /// <summary>Clean up.</summary>
        public new void Dispose();
    }





    // https://github.com/dotnet/runtime/tree/c8acea22626efab11c13778c028975acdc34678f/src/libraries/System.IO.Ports/src/System/IO/Ports
    // https://sparxeng.com/blog/software/must-use-net-system-io-ports-serialport

    public interface ISerialPort : IDisposable
    {
        string PortName { get; set; }
        int BaudRate { get; set; }
        Parity Parity { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }
        int ReadBufferSize { get; set; }
        int WriteBufferSize { get; set; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }
        bool IsOpen { get; }


        Stream BaseStream { get; }


        void Open();
        void Close();
        //void Dispose();
    }

    public class SerialStreamEmu : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanTimeout => true;

        public override bool CanWrite => true;

        public override long Length { get { return _length; } }
        long _length = 0;

        public override long Position
        {
            get { return _position; }
            set { _position = value; }
        }
        long _position = 0;

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

        /////////////////////////////////////////////////////

        public string WriteBuffer { get; private set; } = "";
        public string ReadBuffer { get; private set; } = "";


        // https://stackoverflow.com/questions/16063520/how-do-you-create-an-asynchronous-method-in-c

        //// byte[] bytes = Encoding.UTF8.GetBytes(s);
        //// await stream.WriteAsync(bytes.AsMemory(index, tosend));
        //public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        //{
        //    //for (int i = 0; i < _length;)
        //    //{
        //    //    WriteBuffer += (char)buffer.;
        //    //}
        //    return cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled(cancellationToken) : default;
        //}

        //// var buffer = new byte[BufferSize];
        //// var byteCount = await stream.ReadAsync(buffer);
        //public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        //{
        //    return cancellationToken.IsCancellationRequested ? ValueTask.FromCanceled<int>(cancellationToken) : default;
        //}


        //public override long Seek(long offset, SeekOrigin origin)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void SetLength(long value)
        //{
        //    throw new NotImplementedException();
        //}

        //public override void Write(byte[] buffer, int offset, int count)
        //{
        //    throw new NotImplementedException();
        //}



        // public Task WriteAsync(byte[] buffer, int offset, int count) => WriteAsync(buffer, offset, count, CancellationToken.None);
        // public virtual ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        // public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        // public Task<int> ReadAsync(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count, CancellationToken.None);
        // public virtual Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        // public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        // public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
    }



    public class SerialPortEmu : ISerialPort
    {
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


        SerialStreamEmu _stream = new();

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




    /// <summary>Default comm.</summary>
    public class NullComm : IComm
    {
        #region IComm implementation
        public int ResponseTime { get; set; } = 500;
        public int BufferSize { get; set; } = 4096;
        public string Response { get; private set; } = "Nothing to see here";
        public OpStatus Init(string args) { return OpStatus.Success; }
        public OpStatus Send(string msg) { Response = $"You sent me [{msg}] at {DateTime.Now}"; return OpStatus.Success; }
        public void Dispose() { }
        #endregion
    }
}
