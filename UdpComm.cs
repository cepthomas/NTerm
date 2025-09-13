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
        readonly Logger _logger = LogManager.CreateLogger("UDP");
        readonly string _host;
        readonly int _port;
        readonly ConcurrentQueue<string> _qRecv = new();
        const int CONNECT_TIME = 50;
        const int RESPONSE_TIME = 1000;
        const int BUFFER_SIZE = 4096;
        #endregion

        /// <summary>Constructor.</summary>
        /// <param name="config"></param>
        /// <exception cref="ArgumentException"></exception>
        public UdpComm(List<string> config)
        {
           try
           {
               _host = config[0];
               _port = int.Parse(config[1]);
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

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Send(string req)
        {
            throw new NotSupportedException();
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public string? Receive()
        {
            _qRecv.TryDequeue(out string? res);
            return res;
        }

        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Run(CancellationToken token)
        {

            //udpClient.Connect("www.contoso.com", 11000);

            //// Sends a message to the host to which you have connected.
            //byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");

            //udpClient.Send(sendBytes, sendBytes.Length);

            //// Sends a message to a different host using optional hostname and port parameters.
            //UdpClient udpClientB = new();
            //udpClientB.Send(sendBytes, sendBytes.Length, "AlternateHostMachineName", 11000);

            ////IPEndPoint object will allow us to read datagrams sent from any source.
            //IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

            //// Blocks until a message returns on this socket from a remote host.
            //byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
            //string returnData = Encoding.ASCII.GetString(receiveBytes);

            //// Uses the IPEndPoint object to determine which of these two hosts responded.
            //Console.WriteLine("This is the message you received " + returnData.ToString());
            //Console.WriteLine("This message was sent from " + RemoteIpEndPoint.Address.ToString() +
            //                            " on their port number " + RemoteIpEndPoint.Port.ToString());

            //udpClient.Close();
            //udpClientB.Close();


            UdpClient client = new(_host, _port);
            CommState state = CommState.None;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    //=========== Connect ============//
                    state = CommState.Connect;


                    //=========== Receive ==========//
                    state = CommState.Recv;

                    bool rcvDone = false;
                    //byte[] rxData = new byte[BUFFER_SIZE];

                    while (!rcvDone)
                    {
                        // Get data. If the read time-out expires, Read() throws IOException.
                        var bytes = client.ReceiveAsync(token);//   );// rxData, 0, BUFFER_SIZE);
                        if (bytes.Result.Buffer.Length > 0)
                        {
                            _qRecv.Enqueue(Utils.BytesToString(bytes.Result.Buffer, bytes.Result.Buffer.Length));
                        }
                        else
                        {
                            rcvDone = true;
                        }
                    }

                }
                catch (Exception e)
                {
                    //var stat = Utils.ProcessException(e, _logger);

                    // Connect:
                    // - SocketException - An error occurred when accessing the socket.
                    // - ArgumentNullException - endPoint is null.
                    // - ObjectDisposedException - The UdpClient is closed.
                    // 
                    // ReceiveAsync:
                    // - ObjectDisposedException - The underlying Socket has been closed.
                    // - SocketException - An error occurred when accessing the socket.

                    switch (e)
                    {
                        case SocketException ex: // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                            int[] valid = [10053, 10054, 10060, 10061, 10064];
                            if (valid.Contains(ex.NativeErrorCode))
                            {
                                // Ignore and retry later.
                                //_logger.Debug($"SocketException: Timeout: {ex.NativeErrorCode}");
                            }
                            else
                            {
                                // All other errors are considered fatal - bubble up to App to handle.
                                throw;
                            }
                            break;

                        default: // All other errors are considered fatal - bubble up to App to handle.
                            throw;
                    }
                }

                // Don't be greedy.
                Thread.Sleep(20);
            }
        }



        void Example()
        {
            // The UdpClient class provides simple methods for sending and receiving connectionless UDP datagrams
            // in blocking synchronous mode. Because UDP is a connectionless transport protocol, you do not need to
            // establish a remote host connection prior to sending and receiving data. You do, however, have the
            // option of establishing a default remote host in one of the following two ways:
            //   Create an instance of the UdpClient class using the remote host name and port number as parameters.
            //   Create an instance of the UdpClient class and then call the Connect method.
            // You can use any of the send methods provided in the UdpClient to send data to a remote device. Use the
            // Receive method to receive data from remote hosts.


            // This constructor arbitrarily assigns the local port number.
            UdpClient udpClient = new(11000);
            try
            {
                udpClient.Connect("www.contoso.com", 11000);

                // Sends a message to the host to which you have connected.
                byte[] sendBytes = Encoding.ASCII.GetBytes("Is anybody there?");

                udpClient.Send(sendBytes, sendBytes.Length);

                // Sends a message to a different host using optional hostname and port parameters.
                UdpClient udpClientB = new();
                udpClientB.Send(sendBytes, sendBytes.Length, "AlternateHostMachineName", 11000);

                //IPEndPoint object will allow us to read datagrams sent from any source.
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                // Blocks until a message returns on this socket from a remote host.
                byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                string returnData = Encoding.ASCII.GetString(receiveBytes);

                // Uses the IPEndPoint object to determine which of these two hosts responded.
                Console.WriteLine("This is the message you received " + returnData.ToString());
                Console.WriteLine("This message was sent from " + RemoteIpEndPoint.Address.ToString() +
                                            " on their port number " + RemoteIpEndPoint.Port.ToString());

                udpClient.Close();
                udpClientB.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

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
        }


        /// <summary>IComm implementation.</summary>
        /// <see cref="IComm"/>
        public void Reset()
        {
        }
    }
}
