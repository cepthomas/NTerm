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
        /// <exception cref="ArgumentException"></exception>
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
               throw new ArgumentException(msg);
           }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
        }

        /// <summary>What am I.</summary>
        public override string ToString()
        {
            return ($"UdpComm {_host}:{_port}");
        }
        #endregion

        #region IComm implementation
        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(byte[] td)
        {
            throw new NotSupportedException();
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
        public void Run_orig(CancellationToken token)
        {
            //https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.udpclient

            while (!token.IsCancellationRequested)
            {
                try
                {
                    //=========== Connect ============//
                    using UdpClient client = new(_host, _port);


                    //=========== Send ============//
                    // Not implemented.


                    //=========== Receive ==========//
                    bool rcvDone = false;
                    byte[] rxData = new byte[BUFFER_SIZE];
                    while (!rcvDone)
                    {
                        // Get data.

                        int byteCount = client.Client.Receive(rxData);
                        if (byteCount > 0)
                        {
                            _qRecv.Enqueue(rxData[..byteCount]);
                        }

                        // async
                        //var task = client.ReceiveAsync(token);
                        //if (task.Result.Buffer.Length > 0)
                        //{
                        //    _qRecv.Enqueue(task.Result.Buffer);
                        //}
                        //else
                        //{
                        //    rcvDone = true;
                        //}
                    }
                }
                catch (Exception e)
                {
                    ProcessException(e);
                }

                // Don't be greedy.
                Thread.Sleep(5);
            }
        }

        public void Run(CancellationToken token)
        {
            UdpClient listener = new(_port);
            IPEndPoint groupEP = new(IPAddress.Any, _port);

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for broadcast");
                    byte[] bytes = listener.Receive(ref groupEP);

                    Console.WriteLine($"Received broadcast from {groupEP} :");
                    Console.WriteLine($" {Encoding.ASCII.GetString(bytes, 0, bytes.Length)}");
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                listener.Close();
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
