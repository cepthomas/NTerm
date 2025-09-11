using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


// https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcpclient


namespace NTerm
{
    /// <summary>TCP comm.</summary>
    /// <see cref="IComm"/>
    internal class TcpComm : IComm
    {
        #region Fields
        /// <summary>Logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("TCP");

        /// <summary>Update client.</summary>
        IProgress<string> _progress;

        /// <summary>Config.</summary>
        string _host;

        /// <summary>Config.</summary>
        int _port;

        /// <summary>Send queue.</summary>
        readonly ConcurrentQueue<string> _qsend = new();

        const int CONNECT_TIME = 50;
        const int RESPONSE_TIME = 1000;
        const int BUFFER_SIZE = 4096;
        #endregion

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        /// <exception cref="ArgumentException"></exception>
        public void Init(List<string> config, IProgress<string> progress)
        {
            try
            {
                _progress = progress;
                _host = config[0];
                _port = int.Parse(config[1]);
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
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(string req)
        {
            _qsend.Enqueue(req);
        }


        // Task DoWorkAsync(string data)
        // {
        //     return Task.Run(() => DoWork(data));
        // }

        // void DoWork(string data)
        // {
        //     Console.WriteLine(data);
        //     Thread.Sleep(100);
        // }


        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Run(CancellationToken token)
        {
            //try  // TODO1 needed? or let App take care of it.
            //{
            //}
            //catch (Exception e)
            //{
            //    stat = ProcessException(e);
            //}


            while (!token.IsCancellationRequested)
            {
                // Check connected, reconnect. TODO1
                //=========== Connect ============//
                using var client = new TcpClient();
                client.SendTimeout = RESPONSE_TIME;
                client.SendBufferSize = BUFFER_SIZE;

                var task = client.ConnectAsync(_host, _port);

                if (!task.Wait(CONNECT_TIME, token))
                {
                    //return (OpStatus.ConnectTimeout, "", resp);
                }

                using var stream = client.GetStream();


                // Any to send?
                //=========== Send ============//
                if (_qsend.TryDequeue(out string? s))
                {
                    bool sendDone = false;
                    var txData = Utils.StringToBytes(s);
                    int numToSend = txData.Length;
                    int ind = 0;

                    while (!sendDone)
                    {
                        // Do a chunk.
                        int tosend = numToSend - ind >= client!.SendBufferSize ? client.SendBufferSize : numToSend - ind;

                        // If the send time-out expires, Write() throws SocketException.
                        stream.Write(txData, ind, tosend);

                        ind += tosend;
                        sendDone = ind >= numToSend;
                    }
                }

                // Any received?
                //=========== Receive ==========//
                bool rcvDone = false;
                byte[] rxData = new byte[BUFFER_SIZE];

                while (!rcvDone)
                {
                    // Get response. If the read time-out expires, Read() throws IOException.
                    int byteCount = stream.Read(rxData, 0, BUFFER_SIZE);

                    if (byteCount == 0)
                    {
                        rcvDone = true;
                    }
                    else
                    {
                        _progress.Report(Utils.BytesToString(rxData, byteCount));
                    }
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        OpStatus ProcessException(Exception e) // TODO1
        {
            OpStatus stat;

            // Async ops carry the original exception here.
            if (e is AggregateException)
            {
                e = e.InnerException ?? e;
            }

            switch (e)
            {
                case OperationCanceledException ex:
                    // Usually connect timeout. Ignore and retry later.
                    stat = OpStatus.ResponseTimeout;
                    //_logger.Debug($"OperationCanceledException: Timeout: {ex.Message}");
                    break;

                case SocketException ex:
                    // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                    int[] valid = [10053, 10054, 10060, 10061, 10064];
                    if (valid.Contains(ex.NativeErrorCode))
                    {
                        // Ignore and retry later.
                        stat = OpStatus.ResponseTimeout;
                        //_logger.Debug($"SocketException: Timeout: {ex.NativeErrorCode}");
                    }
                    else
                    {
                        stat = OpStatus.Error;
                        _logger.Error($"SocketException: Error: {ex}");
                    }
                    break;

                case IOException ex:
                    // Usually receive timeout. Ignore and retry later.
                    stat = OpStatus.ResponseTimeout;
                    //_logger.Debug($"IOException: Timeout: {ex.Message}");
                    break;

                case ObjectDisposedException ex:
                    stat = OpStatus.Error;
                    _logger.Error($"ObjectDisposedException: {ex.Message}");
                    break;

                default:
                    // Other errors are considered fatal.
                    stat = OpStatus.Error;
                    _logger.Error($"Fatal exception: {e}");
                    break;
            }

            return stat;
        }

        ///// <summary>IComm implementation.</summary>
        ///// <see cref="IComm"/>
        //public void Reset()
        //{
        //}
    }
}
