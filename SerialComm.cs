﻿using System;
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
    /// <summary>Serial port comm.</summary>
    /// <see cref="IComm"/>
    public class SerialComm : IComm // TODOF needs debug.
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("SER");
        Config _config = new();
        readonly public SerialPort _serialPort = new();
        #endregion

        #region IComm implementation
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

        public (OpStatus stat, string rx) Send(string? tx)
        {
            return SendAsync(tx).Result;
        }

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
        }
        #endregion

        /// <summary>
        /// Does actual work of sending/receiving.
        /// </summary>
        /// <param name="tx"></param>
        /// <returns>OpStatus and Response populated.</returns>
        public async Task<(OpStatus stat, string rx)> SendAsync(string? tx)
        {
            OpStatus stat = OpStatus.Success;
            var rx = "";

            try
            {
                if (_serialPort is null)
                {
                    //_logger.Error($"Serial port is not open");
                    return (OpStatus.Error, "Serial port is not open");
                }

                using var stream = _serialPort.BaseStream;
                // using var stream = new ScriptStream("TODO1", []);

                /////// Send ////////
                if (tx is not null) // check for poll
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(tx);
                    _logger.Debug($"[Client] Writing request [{tx}]");

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

                rx = string.Join("", parts);

                _logger.Debug($"[Client] Server response was [{rx}]");

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
                rx = "Usually receive timeout.";
                stat = OpStatus.Timeout;
            }
            catch (Exception e)
            {
                // Other errors are considered fatal.
                _logger.Error($"Fatal error:{e}");
                rx = $"Fatal error: {e.Message}";
                stat = OpStatus.Error;
            }

            return (stat, rx);
        }
    }
}
