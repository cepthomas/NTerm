using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    /// <summary>Serial port comm.</summary>
    /// <see cref="IComm"/>
    public class SerialComm : IComm // TODO needs debug with hardware.
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("SER");
        Config _config;
        readonly SerialPort _serialPort;
        #endregion

        #region IComm implementation
        public Stream? AltStream { get; set; } = null;

        //public (OpStatus stat, string msg) Init(Config config)
        public SerialComm(Config config)
        {
            _config = config;
            _serialPort = new();
            OpStatus stat;
            string msg = "";

            try
            {
                // Parse the args. "COM1 9600 E|O|N 6|7|8 0|1|1.5"
                var parts = _config.Args;
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
                // _serialPort.Handshake

                // Test args by opening.
                _serialPort.Open();
            }
            catch (Exception e)
            {
                msg = $"Invalid comm args - {e.Message}";
                stat = OpStatus.Error;
                throw new ArgumentException(msg);
            }

        //    return (stat, msg);
        }

        public (OpStatus stat, string msg) Send(string data)
        {
            OpStatus stat = OpStatus.Success;
            string msg = "";

            try
            {
                if (!_serialPort.IsOpen)
                {
                    throw new InvalidOperationException("Serial port is not open");
                }

                using var stream = AltStream ?? _serialPort.BaseStream;
                _logger.Debug($"[Client] Sending [{data.Length}]");
                stream.Write(Utils.StringToBytes(data));
                msg = "SerialComm sent";
            }
            catch (Exception e)
            {
                _logger.Error($"Fatal error:{e}");
                msg = $"Fatal error: {e.Message}";
                stat = OpStatus.Error;
            }

            return (stat, msg);
        }

        public (OpStatus stat, string msg, string data) Receive()
        {
            OpStatus stat = OpStatus.Success;
            string msg = "";
            string data = "";

            try
            {
                if (!_serialPort.IsOpen)
                {
                    throw new InvalidOperationException("Serial port is not open");
                }

                using var stream = AltStream ?? _serialPort.BaseStream;

                // Get response.
                var rx = new byte[_config!.BufferSize];
                int byteCount = stream.Read(rx, 0, _config.BufferSize);
                data = Utils.BytesToString(rx);

                //// if the serial port is unexpectedly closed, throw an exception
                //if (!_serialPort.IsOpen)
                //{
                //    throw new IOException();
                //}
            }
            catch (Exception e)
            {
                // Other errors are considered fatal. ?? IOException
                _logger.Error($"Fatal error:{e}");
                msg = $"Fatal error: {e.Message}";
                stat = OpStatus.Error;
            }

            return (stat, msg, data);
        }

        public void Reset()
        {

        }        

        public void Dispose()
        {
            _serialPort.Close();
            _serialPort.Dispose();
        }
        #endregion
    }
}
