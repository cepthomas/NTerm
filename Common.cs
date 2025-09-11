using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    #region Exceptions

    #endregion

    #region Enums
    /// <summary>Supported flavors.</summary>
    public enum CommType { Null, Tcp, Udp, Serial }

    /// <summary>How did operation turn out?</summary>
    public enum OpStatus { Success, ConnectTimeout, ResponseTimeout, Error }

    /// <summary>Key modifiers</summary>
    public enum KeyMod { Ctrl, Alt, Shift, CtrlShift }
    #endregion

    #region Types
    /// <summary>One keyboard event.</summary>
    /// <param name="Text">The content</param>
    /// <param name="Modifiers">Maybe modifiers</param>
    record CliInput(string Text, ConsoleModifiers Modifiers);

    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Make me one.</summary>
        /// <param name="config"></param>
        /// <param name="progress"></param>
        void Init(List<string> config, IProgress<string> progress);

        /// <summary>Start the comm task.</summary>
        /// <returns>Operation status.</returns>
        void Run(CancellationToken token);

        /// <summary>Send request to the server.</summary>
        /// <param name="req">What to send</param>
        /// <returns>Operation status.</returns>
        void Send(string req);

        //void Dispose();

        ///// <summary>Reset comms, resource management.</summary>
        //void Reset();
    }
    #endregion

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
    }
}
