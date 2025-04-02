using System;
using System.Collections.Generic;
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
    public class SerialComm : IComm // TODOF needs debug with hardware.
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("SER");
        Config _config = new();
        readonly public SerialPort _serialPort = new();
        #endregion

        #region IComm implementation
        public Stream? AltStream { get; set; } = null;

        public (OpStatus stat, string msg) Init(Config config)
        {
            _config = config;
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
                // Handshake

                // Test args by creating client.
                _serialPort.Open();
            }
            catch (Exception)
            {
                msg = "Invalid comm args";
                stat = OpStatus.Error;
            }

            return (stat, msg);
        }

        public (OpStatus stat, byte[] rx) Send(byte[] tx)
        {
            return SendAsync(tx).Result;
        }

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            AltStream?.Dispose();
            AltStream = null;
        }
        #endregion

        /// <summary>
        /// Does actual work of sending/receiving.
        /// </summary>
        /// <param name="tx">What to send. If simple poll request, this will be empty.</param>
        /// <returns>OpStatus and Response populated.</returns>
        public async Task<(OpStatus stat, byte[] rx)> SendAsync(byte[] tx)
        {
            OpStatus stat = OpStatus.Success;
            byte[] rx;

            try
            {
                if (_serialPort is null)
                {
                    //_logger.Error($"Serial port is not open");
                    return (OpStatus.Error, Utils.StringToBytes("Serial port is not open"));
                }

                using var stream = AltStream ?? _serialPort.BaseStream;

                /////// Send ////////
                if (tx.Length > 0)
                {
                    bool sendDone = false;
                    int num = tx.Length;
                    int ind = 0;

                    while (!sendDone)
                    {
                        // Do a chunk.
                        int tosend = num - ind >= _serialPort.WriteBufferSize ? _serialPort.WriteBufferSize : num - ind;
                        _logger.Debug($"[Client] Sending [{tosend}]");

                        await stream.WriteAsync(tx);

                        ind += tosend;
                        sendDone = ind >= num;
                    }
                }

                /////// Receive ////////
                bool rcvDone = false;
                int totalRx = 0;
                byte[] buffer = new byte[_config.BufferSize];

                while (!rcvDone)
                {
                    // Get response.
                    int byteCount = await stream.ReadAsync(buffer, totalRx, _config.BufferSize - totalRx);

                    if (byteCount == 0)
                    {
                        rcvDone = true;
                    }
                    else if (totalRx >= _config.BufferSize)
                    {
                        rcvDone = true;
                        _logger.Warn("SerialComm rx buffer overflow");
                    }
                }

                // Package return.
                rx = new byte[totalRx];

                if (totalRx > 0)
                {
                    Array.Copy(rx, 0, buffer, 0, totalRx);
                    _logger.Trace($"[Client] Server response was [{totalRx}]");
                }
                else
                {
                    stat = OpStatus.NoResp;
                }
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
                rx = Utils.StringToBytes("Usually receive timeout.");
                stat = OpStatus.Timeout;
            }
            catch (Exception e)
            {
                // Other errors are considered fatal.
                _logger.Error($"Fatal error:{e}");
                rx = Utils.StringToBytes($"Fatal error: {e.Message}");
                stat = OpStatus.Error;
            }

            return (stat, rx);
        }
    }
}
