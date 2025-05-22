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


// https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport


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

        const int CONNECT_TIME = 100;
        const int RESPONSE_TIME = 10;
        const int BUFFER_SIZE = 4096;
        #endregion

        /// <summary>
        /// Make me one.
        /// </summary>
        /// <param name="config"></param>
        /// <exception cref="ArgumentException"></exception>
        public SerialComm(Config config)
        {
            _config = config;
            _serialPort = new();

            try
            {
                // Parse the args: "COM1 9600 E|O|N 6|7|8 0|1|1.5"
                var parts = _config.Args;

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
                _serialPort.ReadBufferSize = BUFFER_SIZE;
                _serialPort.WriteBufferSize = BUFFER_SIZE;
                _serialPort.ReadTimeout = RESPONSE_TIME;
                _serialPort.WriteTimeout = RESPONSE_TIME;
                // _serialPort.Handshake?
            }
            catch (Exception e)
            {
                var msg = $"Invalid args: {e.Message}";
                throw new ArgumentException(msg);
            }
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            _serialPort.Close();
            _serialPort.Dispose();
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public (OpStatus stat, string msg) Send(string data)
        {
            OpStatus stat = OpStatus.Success;
            string msg = "";

            try
            {
                stat = EnsureConnect();

                if (stat == OpStatus.Success)
                {
                    using var stream = _serialPort.BaseStream;
                    _logger.Debug($"[Client] Sending [{data.Length}]");
                    stream.Write(Utils.StringToBytes(data));
                    msg = "SerialComm sent";
                }
            }
            catch (Exception e)
            {
                stat = ProcessException(e);
            }

            return (stat, msg);
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public (OpStatus stat, string msg, string data) Receive()
        {
            OpStatus stat = OpStatus.Success;
            string msg = "";
            string data = "";

            try
            {
                stat = EnsureConnect();

                if (stat == OpStatus.Success)
                {
                    using var stream = _serialPort.BaseStream;

                    // Get response.
                    var rx = new byte[BUFFER_SIZE];
                    int byteCount = stream.Read(rx, 0, BUFFER_SIZE);
                    data = Utils.BytesToString(rx);
                }
            }
            catch (Exception e)
            {
                stat = ProcessException(e);
            }

            return (stat, msg, data);
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        OpStatus EnsureConnect()
        {
            var stat = OpStatus.Success;
            if (!_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Open();
                }
                catch (Exception e)
                {
                    stat = ProcessException(e);
                }
            }

            return stat;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        OpStatus ProcessException(Exception e)
        {
            OpStatus stat;
            switch (e)
            {
                default:
                    // Errors are considered fatal.
                    stat = OpStatus.Error;
                    _logger.Error($"Fatal exception: {e}");
                    break;
            }

            return stat;
        }
    }
}
