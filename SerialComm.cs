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
    public class SerialComm : IComm // TODO needs debug.
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("SerialComm");
        ISerialPort? _serialPort = null;
        // SerialPort? _serialPort = null;
        #endregion

        #region IComm implementation
        public int ResponseTime { get; set; } = 500;

        public int BufferSize { get; set; } = 4096;

        public string Response { get; private set; } = "";

        public OpStatus Send(string msg) { return SendAsync(msg).Result; }

        public OpStatus Init(string args)
        {
            _logger.Debug("Available Ports:");
            SerialPort.GetPortNames().ForEach(s => { _logger.Debug($"   {0}", s); });

            // Parse the args. "COM1 9600 E|O|N 6|7|8 0|1|1.5"
            var parts = args.SplitByToken(" ");
            OpStatus stat = parts.Count == 5 ? OpStatus.Success : OpStatus.ConfigError;

            try
            {
                // SerialPort sport = new()
                var sport = new SerialPortEmu()
                {
                    ReadBufferSize = BufferSize,
                    WriteBufferSize = BufferSize,
                    ReadTimeout = ResponseTime,
                    WriteTimeout = ResponseTime
                    // Handshake
                };

                var i = int.Parse(parts[0].Replace("COM", ""));
                sport.PortName = $"COM{i}";

                sport.BaudRate = int.Parse(parts[1]);

                sport.Parity = parts[2] switch
                {
                    "E" => Parity.Even,
                    "O" => Parity.Odd,
                    "N" => Parity.None,
                    _ => (Parity)(-1) // invalid
                };

                sport.DataBits = parts[3] switch
                {
                    "6" => 6,
                    "7" => 7,
                    "8" => 8,
                    _ => -1 // invalid
                };

                sport.StopBits = parts[4] switch
                {
                    "0" => StopBits.None,
                    "1" => StopBits.One,
                    "1.5" => StopBits.OnePointFive,
                    _ => (StopBits)(-1) // invalid
                };

                sport.Open();
                _serialPort = sport;
            }
            catch (Exception)
            {
                _logger.Error($"Invalid comm args: {args}");
                stat = OpStatus.ConfigError;
            }

            return stat;
        }

        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
        }
        #endregion

        /// <summary>
        /// Does actual work of sending/receiving.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>OpStatus and Response populated.</returns>
        public async Task<OpStatus> SendAsync(string request)
        {
            OpStatus res = OpStatus.Success;
            Response = "";

            try
            {
                if (_serialPort is null)
                {
                    _logger.Warn($"Serial port is not open");
                    return OpStatus.Error;
                }

                /////// Send ////////
                using var stream = _serialPort.BaseStream;
                byte[] bytes = Encoding.UTF8.GetBytes(request);
                _logger.Debug($"[Client] Writing request [{request}]");

                bool sendDone = false;
                int num = bytes.Length;
                int ind = 0;

                while (!sendDone)
                {
                    // Do a chunk.
                    int tosend = num - ind >= _serialPort.WriteBufferSize ? _serialPort.WriteBufferSize : num - ind;
                    _logger.Debug($"[Client] Sending [{tosend}]");

                    await stream.WriteAsync(bytes);
                    //await stream.WriteAsync(bytes.AsMemory(ind, tosend));

                    ind += tosend;
                    sendDone = ind >= num;
                }

                /////// Receive ////////
                List<string> parts = [];
                bool rcvDone = false;

                while (!rcvDone)
                {
                    // Get response.
                    var buffer = new byte[BufferSize];

                    // If the read time-out expires, ReadAsync() throws IOException.
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

                Response = string.Join("", parts);

                _logger.Debug($"[Client] Server response was [{Response}]");


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
                Response = "Usually receive timeout.";
                res = OpStatus.Timeout;
            }
            catch (Exception e)
            {
                // Other errors are considered fatal.
                _logger.Error($"Fatal error:{e}");
                Response = $"Fatal error: {e.Message}";
                res = OpStatus.Error;
            }

            return res;
        }
    }
}
