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
        readonly Logger _logger = LogManager.CreateLogger("TCP");
        //readonly Config _config;
        string _host;
        int _port;

        const int CONNECT_TIME = 50;
        const int BUFFER_SIZE = 4096;
        #endregion

        /// <summary>Send string queue.</summary>
        readonly ConcurrentQueue<string> _qsend = new();



// https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap?redirectedfrom=MSDN
//.NET provides the Progress<T> class, which implements IProgress<T>. The Progress<T> class is declared as follows:
// public class Progress<T> : IProgress<T>  
// {  
//     public Progress();  
//     public Progress(Action<T> handler);  
//     protected virtual void OnReport(T value);  
//     public event EventHandler<T>? ProgressChanged;  
// }  

IProgress<string> _progress;


                /// <param name="config"></param>

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        /// <exception cref="ArgumentException"></exception>
        public OpStatus Init(string config, IProgress<string> progress)
        {
            try
            {
                //_host = host;
                //_port = port;
                _progress = progress;

                // Parse the args: "127.0.0.1 59120"
                var parts = config.SplitByToken(" ");

                _host = parts[0];
                _port = int.Parse(parts[1]);
            }
            catch (Exception e)
            {
                var msg = $"Invalid args: {e.Message}";
                throw new ArgumentException(msg);
            }
            return OpStatus.Success;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public OpStatus Send(string req)
        {
            _qsend.Enqueue(req);
            return OpStatus.Success;
        }


        Task DoWorkAsync(string data)
        {
            return Task.Run(() => DoWork(data));
        }

        void DoWork(string data)
        {
            Console.WriteLine(data);

            Thread.Sleep(100);
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public OpStatus Run()
        {
            var t = DoWorkAsync("booga");

            // var workers = new List<IWorker> {new Worker(), new Worker(), new Worker()};
            
            // var tasks = workers.Select(t => t.DoWorkAsync("some data"));

            // Task.WhenAll(tasks).ContinueWith(task => Callback());

            // Console.WriteLine("Waiting");

            return OpStatus.Success;
        
        }


/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////



        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public (OpStatus stat, string msg, string resp) Send_orig(string req)
        {
            OpStatus stat = OpStatus.Success;
            string msg = "";
            string resp = "";

            try
            {
                //=========== Connect ============//
                using var client = new TcpClient();
// from???                client.SendTimeout = _config.ResponseTime;
                client.SendBufferSize = BUFFER_SIZE;

                var task = client.ConnectAsync(_host, _port);

                if (!task.Wait(CONNECT_TIME))
                {
                    return (OpStatus.ConnectTimeout, "", resp);
                }

                using var stream = client.GetStream();

                //=========== Send ============//
                bool sendDone = false;
                var txData = Utils.StringToBytes(req);
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

                //=========== Receive ==========//
                bool rcvDone = false;
                byte[] rxData = new byte[BUFFER_SIZE];
                List<string> respParts = [];

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
                        respParts.Add(Utils.BytesToString(rxData, byteCount));
                    }
                }

                resp = string.Join("", respParts);
            }
            catch (Exception e)
            {
                stat = ProcessException(e);
            }

            return (stat, msg, resp);
        }






        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        OpStatus ProcessException(Exception e)
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
    }
}
