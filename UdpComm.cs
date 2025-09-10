using System;
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
    internal class UdpComm //: IComm
    {
        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("UDP");
        readonly Config _config;
        readonly string _host;
        readonly int _port;

        const int CONNECT_TIME = 50;
        const int BUFFER_SIZE = 4096;
        #endregion

        /// <summary>
        /// Make me one.
        /// </summary>
        /// <param name="config"></param>
        /// <exception cref="ArgumentException"></exception>
        public UdpComm(Config config)
        {
            try
            {
                _config = config;

                // Parse the args: "127.0.0.1 59120"
                var parts = _config.Args;

                _host = parts[0];
                _port = int.Parse(parts[1]);
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
        public (OpStatus stat, string msg, string resp) Send(string req)
        {
            // No such thing. TODO1

            OpStatus stat = OpStatus.Success;

            string msg = "";
            string resp = "";

            try
            {
                //=========== Connect ============//
                using var client = new TcpClient();
                client.SendTimeout = _config.ResponseTime;
                client.SendBufferSize = BUFFER_SIZE;
                var task = client.ConnectAsync(_host, _port);
                if (!task.Wait(CONNECT_TIME)) { return (OpStatus.ConnectTimeout, "", resp); }
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

                /*
                Example: UDP Server using Python - listen
                import socket
                localIP     = "127.0.0.1"
                localPort   = 20001
                bufferSize  = 1024
                msgFromServer       = "Hello UDP Client"
                bytesToSend         = str.encode(msgFromServer)
                # Create a datagram socket
                UDPServerSocket = socket.socket(family=socket.AF_INET, type=socket.SOCK_DGRAM)
                # Bind to address and ip
                UDPServerSocket.bind((localIP, localPort))
                print("UDP server up and listening")

                # Listen for incoming datagrams
                while(True):
                bytesAddressPair = UDPServerSocket.recvfrom(bufferSize)
                message = bytesAddressPair[0]
                address = bytesAddressPair[1]
                clientMsg = "Message from Client:{}".format(message)
                clientIP  = "Client IP Address:{}".format(address)
                print(clientMsg)
                print(clientIP)
                # Sending a reply to client
                UDPServerSocket.sendto(bytesToSend, address)

                Output:
                UDP server up and listening
                Message from Client:b"Hello UDP Server"
                Client IP Address:("127.0.0.1", 51696)
                */


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
