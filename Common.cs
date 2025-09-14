using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    /// <summary>How did operation turn out?</summary>
    public enum CommState { None, Connect, Send, Recv }

    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Start the comm task.</summary>
        void Run(CancellationToken token);

        /// <summary>Send to the server.</summary>
        /// <param name="msg">What to send</param>
        void Send(string msg);

        /// <summary>Receive from the server.</summary>
        /// <returns>Received message or null if none.</returns>
        byte[]? Receive();

        ///// <summary>Receive from the server.</summary>
        ///// <returns>Received message or null if none.</returns>
        //string? Receive();

        /// <summary>Reset comms, resource management.</summary>
        void Reset();
    }

    public class Utils
    {
        public static string BytesToString(byte[] buff, int cnt)
        {
            return Encoding.Default.GetString(buff, 0, cnt);
        }

        public static string BytesToStringReadable(byte[] buff, int cnt)
        {
            List<string> list = [];
            for (int i = 0; i < cnt; i++)
            {
                var c = buff[i];
                list.Add(c.IsReadable() ? ((char)c).ToString() : $"<{c:X}>");
            }
            return string.Join("", list);
        }

        public static byte[] StringToBytes(string s)
        {
            // Valid strings are always convertible.
            return Encoding.Default.GetBytes(s);
        }

        // /// <summary>
        // /// Common processing of exceptions. Does logging etc.
        // /// </summary>
        // /// <param name="e"></param>
        // /// <param name="logger"></param>
        // /// <returns>OpStatus for caller to interpret.</returns>
        // public static OpStatus ProcessException(Exception e, Logger logger)
        // {
        //     OpStatus stat;

        //     // Async ops carry the original exception in inner.
        //     if (e is AggregateException)
        //     {
        //         e = e.InnerException ?? e;
        //     }

        //     switch (e)
        //     {
        //         case OperationCanceledException ex: // Usually connect timeout. Ignore and retry later.
        //             stat = OpStatus.ResponseTimeout;
        //             //_logger.Debug($"OperationCanceledException: Timeout: {ex.Message}");
        //             break;

        //         case SocketException ex: // Some are expected and recoverable. https://learn.microsoft.com/en-us/windows/win32/winsock/windows-sockets-error-codes-2
        //             int[] valid = [10053, 10054, 10060, 10061, 10064];
        //             if (valid.Contains(ex.NativeErrorCode))
        //             {
        //                 // Ignore and retry later.
        //                 stat = OpStatus.ResponseTimeout;
        //                 //_logger.Debug($"SocketException: Timeout: {ex.NativeErrorCode}");
        //             }
        //             else
        //             {
        //                 stat = OpStatus.Error;
        //                 logger.Error($"SocketException: Error: {ex}");
        //             }
        //             break;

        //         case IOException ex: // Usually receive timeout. Ignore and retry later.
        //             stat = OpStatus.ResponseTimeout;
        //             //_logger.Debug($"IOException: Timeout: {ex.Message}");
        //             break;

        //         //case ObjectDisposedException ex: // fatal
        //         //    stat = OpStatus.Error;
        //         //    logger.Error($"ObjectDisposedException: {ex.Message}");
        //         //    break;

        //         default: // Other errors are considered fatal.
        //             stat = OpStatus.Error;
        //             logger.Error($"Fatal exception: {e}");
        //             break;
        //     }

        //     return stat;
        // }
    }
    // /// <summary>How did operation turn out?</summary>
    // public enum OpStatus { Success, ConnectTimeout, ResponseTimeout, Error }

}
