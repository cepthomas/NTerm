using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    #region Types
    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Current state.</summary>
//        CommState State { get; }

        /// <summary>Start the comm task.</summary>
        void Run(CancellationToken token);

        /// <summary>Send to the other end.</summary>
        /// <param name="msg">What to send</param>
        void Send(byte[] msg);

        /// <summary>Receive from the other end.</summary>
        /// <returns>Received message/error or null if none.</returns>
        object? GetReceive();

        /// <summary>Reset comms, resource management.</summary>
        void Reset();
    }


    // /// <summary>Comm has something to tell the app.</summary>
    // /// <param name="Message">Valid message or null</param>
    // /// <param name="Exc">Error or null</param>
    // readonly record struct Received(byte[]? Message, Exception? Exc);


    #endregion
    /// <summary>Comm error processing categories.</summary>
    public enum CommState
    {
        Ok,          // Keep going
        Timeout,     // Try again later - forever
        Recoverable, // Normal bump e.g. server down, power - retry (with limit?)
        Fatal,       // Config error, hard runtime error, it's dead Jim
    }

    /// <summary>Comm error processing categories.</summary>
    public class Defs // Control or CChar or ??
    {
        // Some control characters.
        public const int LF = 10;
        public const int CR = 13;
        public const int NUL = 0;
        public const int ESC = 27;
    }


    public class Utils
    {
        /// <summary>
        /// Format non-readable for human consumption.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static string FormatByte(byte b)
        {
            if (b.IsReadable())
            {
                return ((char)b).ToString();
            }
            else
            {
                Keys k = (Keys)b;
                return k.ToString();
            }
        }

        // /// <summary>
        // /// Common comm exception handler.
        // /// </summary>
        // /// <param name="e"></param>
        // /// <returns></returns>
        // public static CommState ProcessException(Exception e)
        // {
        //     CommState cst;

        //     // All the possible exceptions:
        //     // Exception,Description,Module,Function,State
        //     // ArgumentException,,SER,common,Fatal
        //     // ArgumentNullException,endPoint is null.,UDP,Connect,Fatal
        //     // ArgumentNullException,The host parameter is null.,TCP,ConnectAsync,Fatal
        //     // ArgumentNullException,,SER,common,Fatal
        //     // ArgumentOutOfRangeException,The port parameter is not between MinPort and MaxPort.,TCP,ConnectAsync,Fatal
        //     // ArgumentOutOfRangeException,,SER,common,Fatal
        //     // UnauthorizedAccessException,Access is denied to the port or Already open.,SER,open,Fatal
        //     // InvalidOperationException,The specified port on the current instance of the SerialPort is already open.,SER,open,Fatal
        //     // InvalidOperationException,The NetworkStream does not support writing/reading.,TCP,stream.Write / stream.Read,Fatal
        //     // InvalidOperationException,The specified port is not open.,SER,write/read,Fatal
        //     // InvalidOperationException,The TcpClient is not connected to a remote host.,TCP,GetStream,Fatal
        //     // IOException,The port is in an invalid state. or the parameters passed from this SerialPort object were invalid.,SER,open,Fatal
        //     // IOException,An error occurred when accessing the socket or There was a failure while writing/reading to the network.,TCP,stream.Write / stream.Read,Recoverable
        //     // ObjectDisposedException,TcpClient is closed.,TCP,ConnectAsync,Recoverable
        //     // ObjectDisposedException,The NetworkStream is closed.,TCP,stream.Write / stream.Read,Recoverable
        //     // ObjectDisposedException,The TcpClient has been closed.,TCP,GetStream,Recoverable
        //     // ObjectDisposedException,The UdpClient is closed.,UDP,Connect,Recoverable
        //     // ObjectDisposedException,The underlying Socket has been closed.,UDP,ReceiveAsync,Recoverable
        //     // OperationCanceledException,The cancellation token was canceled. Exception in returned task.,TCP,ConnectAsync,Recoverable
        //     // SocketException,An error occurred when accessing the socket.,TCP,ConnectAsync,SPECIAL
        //     // SocketException,An error occurred when accessing the socket.,UDP,Connect,SPECIAL
        //     // SocketException,An error occurred when accessing the socket.,UDP,ReceiveAsync,SPECIAL
        //     // TimeoutException,The operation did not complete before the timeout period ended.,SER,write/read,Timeout

        //     // Async ops carry the original exception in inner.
        //     if (e is AggregateException)
        //     {
        //         e = e.InnerException ?? e;
        //     }

        //     switch (e)
        //     {
        //         case OperationCanceledException:
        //         case ObjectDisposedException:
        //         case IOException:
        //             cst = CommState.Recoverable;
        //             break;

        //         case TimeoutException:
        //             cst = CommState.Timeout;
        //             break;

        //         case SocketException ex:
        //             // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
        //             int[] valid = [10053, 10054, 10060, 10061, 10064];
        //             cst = valid.Contains(ex.NativeErrorCode) ? CommState.Recoverable : CommState.Fatal;
        //             break;

        //         case ArgumentNullException:
        //         case ArgumentOutOfRangeException:
        //         case ArgumentException:
        //         case UnauthorizedAccessException:
        //         case InvalidOperationException:
        //         default:
        //             cst = CommState.Fatal;
        //             break;
        //     }

        //     return cst;
        // }
    }
}
