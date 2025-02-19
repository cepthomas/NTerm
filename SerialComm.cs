using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    public class SerialComm(ISerialPort? sport) : IComm // TODO needs debug.
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("SerialComm");
        readonly ISerialPort _serialPort = sport ?? new RealSerialPort();
        Config? _config;
        #endregion

        #region IComm implementation
        public (OpStatus stat, string resp) Init(Config config)
        {
            _config = config;
            var resp = "";

            OpStatus stat;
            try
            {
                // Parse the args. "COM1 9600 E|O|N 6|7|8 0|1|1.5"
                var parts = _config.Args.SplitByToken(" ");
                stat = parts.Count == 5 ? OpStatus.Success : OpStatus.Error;

                var i = int.Parse(parts[0].Replace("COM", ""));
                _serialPort.PortName = $"COM{i}";

                _serialPort.BaudRate = int.Parse(parts[1]);

                _serialPort.Parity = parts[2] switch
                {
                    "E" => Parity.Even,
                    "O" => Parity.Odd,
                    "N" => Parity.None,
                    _ => throw new ArgumentException($"Invalid parity:{parts[2]}"),
                };

                _serialPort.DataBits = parts[3] switch
                {
                    "6" => 6,
                    "7" => 7,
                    "8" => 8,
                    _ => throw new ArgumentException($"Invalid data bits:{parts[3]}"),
                };

                _serialPort.StopBits = parts[4] switch
                {
                    "0" => StopBits.None,
                    "1" => StopBits.One,
                    "1.5" => StopBits.OnePointFive,
                    _ => throw new ArgumentException($"Invalid stop bits:{parts[4]}"),
                };

                // Other params.
                _serialPort.ReadBufferSize = _config.BufferSize;
                _serialPort.WriteBufferSize = _config.BufferSize;
                _serialPort.ReadTimeout = _config.ResponseTime;
                _serialPort.WriteTimeout = _config.ResponseTime;
                // Handshake

                _serialPort.Open();
            }
            catch (Exception)
            {
                resp = "Invalid comm args";
                stat = OpStatus.Error;
            }

            return (stat, resp);
        }

        public (OpStatus stat, string resp) Send(string? cmd) { return SendAsync(cmd).Result; }

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
        }
        #endregion

        /// <summary>
        /// Does actual work of sending/receiving.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>OpStatus and Response populated.</returns>
        public async Task<(OpStatus stat, string resp)> SendAsync(string? msg)
        {
            OpStatus stat = OpStatus.Success;
            var resp = "";

            try
            {
                if (_serialPort is null)
                {
                    //_logger.Error($"Serial port is not open");
                    return (OpStatus.Error, "Serial port is not open");
                }

                using var stream = _serialPort.BaseStream;

                /////// Send ////////
                if (msg is not null) // check for poll
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(msg);
                    _logger.Debug($"[Client] Writing request [{msg}]");

                    bool sendDone = false;
                    int num = bytes.Length;
                    int ind = 0;

                    while (!sendDone)
                    {
                        // Do a chunk.
                        int tosend = num - ind >= _serialPort.WriteBufferSize ? _serialPort.WriteBufferSize : num - ind;
                        _logger.Debug($"[Client] Sending [{tosend}]");

                        await stream.WriteAsync(bytes);

                        ind += tosend;
                        sendDone = ind >= num;
                    }
                }

                /////// Receive ////////
                List<string> parts = [];
                bool rcvDone = false;

                while (!rcvDone)
                {
                    // Get response.
                    var buffer = new byte[_config.BufferSize];

                    var byteCount = await stream.ReadAsync(buffer);

                    if (byteCount > 0)
                    {
                        var s = Encoding.UTF8.GetString(buffer, 0, byteCount);
                        parts.Add(s);
                    }
                    else
                    {
                        rcvDone = true;
                    }
                }

                resp = string.Join("", parts);

                _logger.Debug($"[Client] Server response was [{resp}]");

                // if the serial port is unexpectedly closed, throw an exception
                if (!_serialPort.IsOpen)
                {
                    throw new IOException();
                }
            }
            catch (TimeoutException e)
            {
                // Ignore and retry later.
                _logger.Debug($"{e.Message}: {e}");
                resp = "Usually receive timeout.";
                stat = OpStatus.Timeout;
            }
            catch (Exception e)
            {
                // Other errors are considered fatal.
                _logger.Error($"Fatal error:{e}");
                resp = $"Fatal error: {e.Message}";
                stat = OpStatus.Error;
            }

            return (stat, resp);
        }
    }

    /// <summary>Real serial port implementation.</summary>
    public class RealSerialPort : ISerialPort
    {
        readonly SerialPort _serialPort = new();

        #region ISerialPort implementation
        public int ReadBufferSize { get => _serialPort.ReadBufferSize; set => _serialPort.ReadBufferSize = value; }
        public int WriteBufferSize { get => _serialPort.WriteBufferSize; set => _serialPort.WriteBufferSize = value; }
        public int ReadTimeout { get => _serialPort.ReadTimeout; set => _serialPort.ReadTimeout = value; }
        public int WriteTimeout { get => _serialPort.WriteTimeout; set => _serialPort.WriteTimeout = value; }
        public string PortName { get => _serialPort.PortName; set => _serialPort.PortName = value; }
        public int BaudRate { get => _serialPort.BaudRate; set => _serialPort.BaudRate = value; }
        public Parity Parity { get => _serialPort.Parity; set => _serialPort.Parity = value; }
        public int DataBits { get => _serialPort.DataBits; set => _serialPort.DataBits = value; }
        public StopBits StopBits { get => _serialPort.StopBits; set => _serialPort.StopBits = value; }
        public bool IsOpen { get => _serialPort.IsOpen; }
        public Stream BaseStream { get { return _serialPort.BaseStream; } }
        public void Close() { _serialPort.Close(); }
        public void Dispose() { _serialPort.Dispose(); }
        public void Open() { _serialPort.Open(); }
        #endregion
    }
}
