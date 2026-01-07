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


namespace NTerm
{
    /// <summary>UDP comm.</summary>
    /// <see cref="IComm"/>
    internal class UdpComm : IComm
    {
        #region Fields
        readonly string _host;
        readonly int _port;
        readonly ConcurrentQueue<byte[]> _qRecv = new();
        const int BUFFER_SIZE = 4096;
        #endregion

        #region Lifecycle
        /// <summary>Constructor.</summary>
        /// <param name="config"></param>
        /// <exception cref="ConfigException"></exception>
        public UdpComm(List<string> config)
        {
           try
           {
               _host = config[1];
               _port = int.Parse(config[2]);
           }
           catch (Exception e)
           {
               var msg = $"Invalid args: {e.Message}";
               throw new ConfigException(msg);
           }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return $"UdpComm {_host}:{_port}";
        }
        #endregion

        #region IComm implementation
        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(byte[] td)
        {
            _qRecv.Enqueue([]);
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public byte[]? Receive()
        {
            _qRecv.TryDequeue(out byte[]? res);
            return res;
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public event EventHandler<NotifEventArgs>? Notif;
        #endregion

        /// <summary>Main work loop.</summary>
        /// <see cref="IComm"/>
        public void Run(CancellationToken token)
        {
            //=========== Connect ============//
            using var listener = new UdpClient(_port);
            IPEndPoint ep = new(IPAddress.Any, _port);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    //=========== Receive ==========//
                    // sync
                    //byte[] bytes = listener.Receive(ref ep);
                    //Console.WriteLine($"Received broadcast from {ep} :");

                    // async(ish)
                    var task = listener.ReceiveAsync(token);
                    if (task.IsCompleted && task.IsCompletedSuccessfully)
                    {
                        byte[] bytes = task.Result.Buffer;
                        Console.WriteLine($"{Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                    }
                }
            }
            catch (Exception e)
            {
                ProcessException(e);
            }
        }

        #region Internals
        /// <summary>
        /// Handle errors.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        bool ProcessException(Exception e)
        {
            // Connect:
            // - SocketException - An error occurred when accessing the socket.
            // - ArgumentNullException - endPoint is null.
            // - ObjectDisposedException - The UdpClient is closed.
            // 
            // ReceiveAsync:
            // - ObjectDisposedException - The underlying Socket has been closed.
            // - SocketException - An error occurred when accessing the socket.

            bool fatal = false;

            switch (e)
            {
                case SocketException ex: // Some are expected and recoverable.
                    // https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                    int[] valid = [10053, 10054, 10060, 10061, 10064];
                    if (valid.Contains(ex.NativeErrorCode))
                    {
                        // Ignore and retry later.
                    }
                    else
                    {
                        // Just Notify/log and carry on.
                        Notif?.Invoke(this, new(Cat.None, e.Message));
                    }
                    break;

                default:
                    // Just Notify/log and carry on.
                    Notif?.Invoke(this, new(Cat.None, e.Message));
                    break;
            }

            return fatal;
        }
        #endregion
    }
}
