using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


// https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient


// TODO support Length-Prefix delim? (Most Common Overall): Prefixes each message with a fixed-size integer header
// - CRLF (\r\n) (Most Common Text Delimiter): Uses a Carriage Return followed by a Line Feed.
//   It is widely used in classic text-based protocols running over TCP, such as HTTP, SMTP, POP3, and IMAP.
// - Single Newline (\n or \r): Commonly used for line-delimited streaming, chat protocols, and log shipping (like Syslog).


namespace NTerm
{
    /// <summary>TCP comm.</summary>
    /// <see cref="IComm"/>
    internal class TcpComm : IComm
    {
        #region Fields
        readonly string _host;
        readonly int _port;
        readonly ConcurrentQueue<string> _qSend = new();
        const int CONNECT_TIME = 50;
        const int RESPONSE_TIME = 1000;
        const int BUFFER_SIZE = 4096;
        /// <summary>Message delimiter</summary>
//        readonly Delim _delim;
        #endregion


        readonly byte? _delim;
        readonly byte? _delim2;

        /// <summary>Characters used for comm control.</summary>
        //public enum Delim { NONE, NULL, ESC, LF, CR, CRLF }







        #region Lifecycle
        /// <summary>Constructor.</summary>
        /// <param name="config"></param>
        /// <exception cref="ConfigException"></exception>
        public TcpComm(List<string> config)
        {
            try
            {
                _host = config[1];
                _port = int.Parse(config[2]);
                if (config.Count > 3)
                {
                   // _delim = Enum.Parse<Delim>(config[3], true);

                    switch (config[3].ToUpper())
                    {
                        case "NONE": break;
                        case "NULL": _delim = 0x00; break;
                        case "ESC": _delim = 0x1B; break;
                        case "CR": _delim = 0x0D; break;
                        case "LF": _delim = 0x0A; break;
                        case "CRLF": _delim = 0x0A; _delim2 = 0x0D; break;
                        default: throw new ConfigException(config[3]);
                    }

                    //byte _cdelim = _delim switch
                    //{
                    //    Delim.NONE => 0xFF,
                    //    Delim.NULL => 0x00,
                    //    Delim.ESC => 0x1B,
                    //    Delim.CR => 0x0D,
                    //    Delim.LF => 0x0A,
                    //    Delim.CRLF => 0x0A,
                    //    _ => 0xFF
                    //};
                }
            }
            catch (Exception e)
            {
                var msg = $"Invalid arg: {e.Message}";
                throw new ConfigException(msg);
            }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
        }

        /// <summary>Send help.</summary>
        public static List<string> Usage()
        {
            return
            [
                "tcp host port [delim]",
                "host: like 127.0.0.1",
                "port: port to talk on",
                "delim: message delimiter=NONE|NULL|ESC|LF|CR|CRLF default is CRLF"
            ];
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return ($"TcpComm {_host}:{_port}");
        }
        #endregion

        #region IComm implementation
        /// <see cref="IComm"/>
        public void Send(string td)
        {
            _qSend.Enqueue(td);
        }

        /// <see cref="IComm"/>
        public void Reset()
        {
            // Nothing.
        }

        /// <summary>Main work loop.</summary>
        /// <see cref="IComm"/>
        public async Task Run(CancellationToken token, IProgress<byte[]> progress)
        {
            bool done = false;

            while (!done)
            {
                try
                {
                    //=========== Connect ============//
                    using var client = new TcpClient();
                    client.SendTimeout = RESPONSE_TIME;
                    client.SendBufferSize = BUFFER_SIZE;

                    var task = client.ConnectAsync(_host, _port);
                    if (!task.Wait(CONNECT_TIME, token))
                    {
                        throw new TimeoutException();
                    }
                    using var stream = client.GetStream();

                    // Start a background task to continuously read server messages
                    // Task receiveTask = Receive(stream, token, progress);
                    // Fire-and-forget the infinite background task
                    // Task.Run(() => DoWorkAsync(_cts.Token));
                    _ = Task.Run(() => Receive(stream, token, progress));

                    //=========== Sending? ============//
                    // Main loop for sending data from console input
                    while (!token.IsCancellationRequested)
                    {
                        if (_qSend.TryDequeue(out string? s))
                        {
                            // Add terminator maybe.
                            if (_delim2 is not null) s += (char)_delim2;
                            if (_delim is not null) s += (char)_delim;
                            var td = Encoding.UTF8.GetBytes(s);
                            await stream.WriteAsync(td, token);
                        }

                        // Don't be greedy.
                        await Task.Delay(10, token);
                    }
                }
                catch (Exception e)
                {
                    // What happened?
                    done = Common.ProcessException(e);
                }

                // Don't be greedy.
                await Task.Delay(10, token);
            }
        }
        #endregion

        #region Privates
        /// <summary>
        /// Background task to listen for incoming messages.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="token"></param>
        /// <param name="progress"></param>
        /// <returns>The new Task</returns>
        async Task Receive(NetworkStream stream, CancellationToken token, IProgress<byte[]> progress)
        {
            byte[] recvData = new byte[BUFFER_SIZE];
            bool done = false;

            //// Distilled version for processing data before unpacketing.
            //byte _cdelim = _delim switch
            //{
            //    Delim.NONE => 0xFF,
            //    Delim.NULL => 0x00,
            //    Delim.ESC => 0x1B,
            //    Delim.CR => 0x0D,
            //    Delim.LF => 0x0A,
            //    Delim.CRLF => 0x0A,
            //    _ => 0xFF
            //};

            // Collected data while looking for delimiter.
            List<byte> buffer = [];

            while (!done && !token.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("start read async");
                    // Read incoming bytes asynchronously
                    int numRead = await stream.ReadAsync(recvData, token);

                    // If ReadAsync returns 0, the server closed the connection.
                    if (numRead == 0)
                    {
                        Console.WriteLine("server closed conn");
                        break;
                    }

                    // Decode the message.
                    if (_delim is null) //Delim.NONE)
                    {
                        // No delim, just deliver whatever arrived.
                        progress.Report(recvData);
                    }
                    else
                    {
                        // Look for delimiter or just buffer it. TODO1 clean up this logic
                        bool isDelim = false;
                        for (int i = 0; i < numRead; i++)
                        {
                            if (recvData[i] == _delim)
                            {
                                if (_delim2 is not null)
                                {
                                    if (buffer.Count > 0 && buffer.Last() == _delim2)
                                    {
                                        isDelim = true;
                                        buffer.RemoveAt(buffer.Count - 1); // trim
                                    }
                                }
                                else
                                {
                                    isDelim = true;
                                }

                                if (isDelim)
                                {
                                    // Complete line so process it.
                                    //buffer.RemoveAt(buffer.Count - 1); // trim
                                    var srecv = buffer.ToArray();
                                    progress.Report(srecv);
                                    buffer.Clear();
                                }
                            }
                            else
                            {
                                // Just add to buffer.
                                buffer.Add(recvData[i]);
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    // What happened?
                    done = Common.ProcessException(e);
                }
            }
        }
        #endregion
    }
}
