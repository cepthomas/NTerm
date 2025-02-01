using Ephemera.NBagOfTricks.Slog;
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


namespace NTerm
{
    public class SerialComm : IComm
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("SerialComm");
        SerialPort? _serialPort = null;
        #endregion

        #region IComm implementation
        public int ResponseTime { get; set; } = 500;

        public int BufferSize { get; set; } = 4096;

        public string Response { get; private set; } = "";

        public OpStatus Send(string msg) { return SendAsync(msg).Result; }

        public OpStatus Init(string args)//TODO
        {
            OpStatus stat = OpStatus.Success;

            //try
            //{
            //    Console.WriteLine("Available Ports:");
            //    foreach (string s in SerialPort.GetPortNames())
            //    {
            //        Console.WriteLine("   {0}", s);
            //    }

            //    _serialPort = new()
            //    {
            //        PortName = name,
            //        BaudRate = baud,
            //        Parity = parity,
            //        DataBits = dataBits,
            //        StopBits = stopBits,
            //        //_serialPort.Handshake = 
            //        ReadBufferSize = BufferSize,
            //        WriteBufferSize = BufferSize,
            //        ReadTimeout = ResponseTime,
            //        WriteTimeout = ResponseTime
            //    };

            //    _serialPort.Open();
            //}
            //catch (Exception e)
            //{
            //    // Fatal.
            //    _logger.Error($"Fatal error:{e}");
            //    Response = $"Fatal error: {e.Message}";
            //    //res = OpStatus.Error;
            //}

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
        /// Does actual work of sending/receiving using async.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>OpStatus and Response populated.</returns>
        public async Task<OpStatus> SendAsync(string request)
        {
            OpStatus res = OpStatus.Success;
            Response = "";

            try
            {
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

                    await stream.WriteAsync(bytes, ind, tosend);

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
                    var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);

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
