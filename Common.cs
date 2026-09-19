using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    #region Types
    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Start the comm task.</summary>
        /// <param name="token">Cancel token.</param>
        /// <param name="progress">Report received data. It could possibly have non-printables so let client deal with that.</param>
        /// <returns>The new Task</returns>
        Task Run(CancellationToken token, IProgress<byte[]> progress);

        /// <summary>Send user input.</summary>
        /// <param name="td">What to send</param>
        void Send(string td);

        /// <summary>Reset comms, resource management.</summary>
        void Reset();
    }
    #endregion

    public static class Common
    {
        /// <summary>
        /// Lots of exceptions can happen with socket ops. Digest them.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>True if done</returns>
        /// <exception>Originating error if fatal</exception>
        public static bool ProcessException(Exception e)
        {
            bool quit = false;

            // All the possible socket exceptions per MS docs:
            // Exception                  ,Description                                                     ,Comm,Function         ,State      
            // ArgumentException          ,                                                                ,SER ,common           ,Fatal      
            // ArgumentNullException      ,endPoint is null.                                               ,UDP ,Connect          ,Fatal      
            // ArgumentNullException      ,The host parameter is null.                                     ,TCP ,ConnectAsync     ,Fatal      
            // ArgumentNullException      ,                                                                ,SER ,common           ,Fatal      
            // ArgumentOutOfRangeException,The port parameter is not between MinPort and MaxPort.          ,TCP ,ConnectAsync     ,Fatal      
            // ArgumentOutOfRangeException,                                                                ,SER ,common           ,Fatal      
            // UnauthorizedAccessException,Access is denied to the port or Already open.                   ,SER ,open             ,Fatal      
            // InvalidOperationException  ,The specified port is already open.                             ,SER ,open             ,Fatal      
            // InvalidOperationException  ,The NetworkStream does not support writing/reading.             ,TCP ,stream.Write/Read,Fatal      
            // InvalidOperationException  ,The specified port is not open.                                 ,SER ,write/read       ,Fatal      
            // InvalidOperationException  ,The TcpClient is not connected to a remote host.                ,TCP ,GetStream        ,Fatal      
            // IOException                ,The port is in an invalid state or invalid.                     ,SER ,open             ,Fatal      
            // IOException                ,Error when accessing the socket or network read/write failure.  ,TCP ,stream.Write/Read,Recoverable
            // ObjectDisposedException    ,TcpClient is closed.                                            ,TCP ,ConnectAsync     ,Recoverable
            // ObjectDisposedException    ,The NetworkStream is closed.                                    ,TCP ,stream.Write/Read,Recoverable
            // ObjectDisposedException    ,The TcpClient has been closed.                                  ,TCP ,GetStream        ,Recoverable
            // ObjectDisposedException    ,The UdpClient is closed.                                        ,UDP ,Connect          ,Recoverable
            // ObjectDisposedException    ,The underlying Socket has been closed.                          ,UDP ,ReceiveAsync     ,Recoverable
            // OperationCanceledException ,The cancellation token was canceled. Exception in returned task.,TCP ,ConnectAsync     ,Recoverable
            // SocketException            ,Error when accessing the socket.                                ,TCP ,ConnectAsync     ,SPECIAL    
            // SocketException            ,Error when accessing the socket.                                ,UDP ,Connect          ,SPECIAL    
            // SocketException            ,Error when accessing the socket.                                ,UDP ,ReceiveAsync     ,SPECIAL    
            // TimeoutException           ,The operation did not complete before the timeout period ended. ,SER ,write/read       ,Timeout    

            // Async ops carry the original exception in inner.
            e = e.InnerException ?? e;
            //if (e is AggregateException) { e = e.InnerException ?? e; }

            switch (e)
            {
                case TaskCanceledException:
                    quit = true;
                    break;

                case OperationCanceledException:
                case ObjectDisposedException:
                case IOException:
                case TimeoutException:
                    quit = false;
                    break;

                case SocketException ex:
                    // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
                    int[] valid = [10053, 10054, 10060, 10061, 10064];
                    if (valid.Contains(ex.NativeErrorCode))
                    {
                        quit = false;
                    }
                    else
                    {
                        throw e;
                    }
                    break;

                // case ArgumentNullException:
                // case ArgumentOutOfRangeException:
                // case ArgumentException:
                // case UnauthorizedAccessException:
                // case InvalidOperationException:
                default:
                    throw e;
            }

            return quit;
        }

        /// <summary>
        /// Format non-readable for human consumption.
        /// </summary>
        /// <param name="bin"></param>
        /// <returns>Readable string</returns>
        public static string MakeReadable(byte[] bin)
        {
            List<string> buff = [];
            bin.ForEach(b => buff.Add(MakeReadable(b)));
            return string.Join("", buff);
        }

        /// <summary>
        /// Format non-readable for human consumption.
        /// </summary>
        /// <param name="b">What to look at</param>
        /// <param name="hexDelim">Visual marker for binary</param>
        /// <returns></returns>
        public static string MakeReadable(byte b, char hexDelim = '|')
        {
            string s = "";
            s = b switch
            {
                byte ba when ba >= ' ' && ba <= '~' => ((char)b).ToString(),
                0 => "<NUL>",
                9 => "<TAB>",
                10 => "<LF>",
                13 => "<CR>",
                27 => "<ESC>",
                _ => $"{hexDelim}{b:X2}{hexDelim}",
            };
            return s;
        }
    }
}
