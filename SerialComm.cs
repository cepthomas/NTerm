using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


// https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport


namespace NTerm
{
    /// <summary>Serial port comm.</summary>
    /// <see cref="IComm"/>
    public class SerialComm : IComm // TODO needs finishing with hardware.
    {
        #region Fields
        readonly SerialPort _serialPort;
        readonly ConcurrentQueue<string> _qSend = new();
        readonly ConcurrentQueue<byte[]> _qRecv = new();
        const int RESPONSE_TIME = 10;
        const int BUFFER_SIZE = 4096;
        #endregion

        #region Lifecycle
        /// <summary>Constructor.</summary>
        /// <param name="config"></param>
        /// <exception cref="IniSyntaxException"></exception>
        public SerialComm(List<string> config)
        {
            _serialPort = new();

            try
            {
                // Parse the args: COM1 9600 8N1 => E|O|N 6|7|8 0|1|15
                _serialPort.PortName = config[1];

                _serialPort.BaudRate = int.Parse(config[2]);

                _serialPort.DataBits = config[3][0] switch
                {
                    '6' => 6,
                    '7' => 7,
                    '8' => 8,
                    _ => throw new ArgumentException($"Invalid data bits:{config[2]}"),
                };

                _serialPort.Parity = config[3][1] switch
                {
                    'E' => Parity.Even,
                    'O' => Parity.Odd,
                    'N' => Parity.None,
                    _ => throw new ArgumentException($"Invalid parity:{config[2]}"),
                };

                _serialPort.StopBits = config[3][2] switch
                {
                    '0' => StopBits.None,
                    '1' => StopBits.One,
                    //'15' => StopBits.OnePointFive,
                    _ => throw new ArgumentException($"Invalid stop bits:{config[2]}"),
                };

                // Other params.
                _serialPort.ReadBufferSize = BUFFER_SIZE;
                _serialPort.WriteBufferSize = BUFFER_SIZE;
                _serialPort.ReadTimeout = RESPONSE_TIME;
                _serialPort.WriteTimeout = RESPONSE_TIME;
                // _serialPort.Handshake?
            }
            catch (Exception e)
            {
                var msg = $"Invalid args: {e.Message}";
                throw new IniSyntaxException(msg, -1);
            }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
            _serialPort.Close();
            _serialPort.Dispose();
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return ($"SerialComm {_serialPort}");
        }
        #endregion

        #region IComm implementation
        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(string req)
        {
            _qSend.Enqueue(req);
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public byte[]? Receive()
        {
            _qRecv.TryDequeue(out byte[]? res);
            return res;
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Run(CancellationToken token)
        {
            CommState state = CommState.None;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    //=========== Connect ============//
                    state = CommState.Connect;

                    if (!_serialPort.IsOpen)
                    {
                        _serialPort.Open();
                    }

                    //=========== Send ============//
                    state = CommState.Send;

                    while (_qSend.TryDequeue(out string? s))
                    {
                        _serialPort.Write(s);
                    }

                    //=========== Receive ==========//
                    state = CommState.Recv;

                    var rxdata = new byte[BUFFER_SIZE];
                    int byteCount = _serialPort.Read(rxdata, 0, BUFFER_SIZE);

                    if (byteCount > 0)
                    {
                        _qRecv.Enqueue(rxdata);
                    }
                }
                catch (Exception e)
                {
                    // All fatal except TimeoutException.
                    // common:
                    // - ArgumentOutOfRangeException
                    // - ArgumentException
                    // - ArgumentNullException
                    // open:
                    // - UnauthorizedAccessException  Access is denied to the port. -or- Already open.
                    // - IOException  The port is in an invalid state. -or- the parameters passed from this SerialPort object were invalid.
                    // - InvalidOperationException  The specified port on the current instance of the SerialPort is already open.
                    // write:
                    // - InvalidOperationException - The specified port is not open.
                    // - TimeoutException - The operation did not complete before the time-out period ended.
                    // read:
                    // - InvalidOperationException - The specified port is not open.
                    // - TimeoutException - No bytes were available to read.

                    if (e is TimeoutException)
                    {
                        // Handle timeout, or just keep trying.
                    }
                    else
                    {
                        // Fatal - bubble up to App to handle.
                        throw;
                    }
                }

                // Don't be greedy.
                Thread.Sleep(5);
            }
        }
        #endregion
    }
}
