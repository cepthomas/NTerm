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
    public class SerialComm : IComm // TODO need hardware for test.
    {
        #region Fields
        readonly SerialPort _serialPort;
        readonly ConcurrentQueue<byte[]> _qSend = new();
        const int RESPONSE_TIME = 10;
        const int BUFFER_SIZE = 4096;
        readonly string _config;
        #endregion

        #region Lifecycle
        /// <summary>Constructor.</summary>
        /// <param name="config"></param>
        /// <exception cref="ConfigException"></exception>
        public SerialComm(List<string> config)
        {
            _serialPort = new();
            _config = string.Join(' ', config[1..]);

            try
            {
                _serialPort.PortName = config[1];
                _serialPort.BaudRate = int.Parse(config[2]);
                var framing = config.Count > 3 ? config[3] : "8N1";

                _serialPort.DataBits = framing[0] switch
                {
                    '6' => 6,
                    '7' => 7,
                    '8' => 8,
                    _ => throw new ConfigException($"Invalid data bits: {framing}"),
                };

                _serialPort.Parity = framing[1] switch
                {
                    'E' => Parity.Even,
                    'O' => Parity.Odd,
                    'N' => Parity.None,
                    _ => throw new ConfigException($"Invalid parity: {framing}"),
                };

                _serialPort.StopBits = framing[2] switch
                {
                    '1' => StopBits.One,
                    '2' => StopBits.Two,
                    _ => throw new ConfigException($"Invalid stop bits: {framing}"),
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
                var msg = $"Invalid arg: {e.Message}";
                _config = "invalid";
                throw new ConfigException(msg);
            }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
            _serialPort.Close();
            _serialPort.Dispose();
        }

        /// <summary>What am I.</summary>
        /// <summary>Send help.</summary>
        public static List<string> Usage()
        {
            return
            [
                "ser port baud [framing]",
                "port: like COM99",
                "baud: baud rate",
                "framing: bits=6|7|8 parity=E|O|N stop bits=1|2 default is 8N1"
            ];
        }

        public override string ToString()
        {
            return $"SerialComm {_config[1..]} ";
        }
        #endregion

        #region IComm implementation
        /// <see cref="IComm"/>
        public void Send(byte[] req)
        {
            _qSend.Enqueue(req);
        }

        /// <see cref="IComm"/>
        public void Reset()
        {
        }

        /// <summary>Main work loop.</summary>
        /// <see cref="IComm"/>
        public async Task Run(CancellationToken token, IProgress<byte[]> progress)
        {
            bool done = false;
            //_logger.Info("Run start");

            while (!done)
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    //=========== Connect ============//
                    if (!_serialPort.IsOpen)
                    {
                        _serialPort.Open();
                    }

                    //=========== Send ============//
                    while (_qSend.TryDequeue(out byte[]? td))
                    {
                        _serialPort.Write(td, 0, td.Length);
                    }

                    //=========== Receive ==========//
                    var recvdata = new byte[BUFFER_SIZE];
                    int byteCount = _serialPort.Read(recvdata, 0, BUFFER_SIZE);

                    if (byteCount > 0)
                    {
                        progress.Report(recvdata);
                    }
                }
                catch (Exception e)
                {
                    // What happened?
                    var res = Common.ProcessException(e);

                    switch (res.cst)
                    {
                        case CommState.Ok:
                        case CommState.Timeout:
                        case CommState.Recoverable:
                            // Continue running.
                            break;

                        case CommState.Stop:
                            done = true;
                            break;

                        case CommState.Fatal:
                            throw (res.e);
                    }
                }

                // Don't be greedy.
                await Task.Delay(10, token);
            }
        }
        #endregion
    }
}
