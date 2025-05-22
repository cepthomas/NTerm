using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    #region Exceptions

    #endregion

    #region Enums
    /// <summary>Supported flavors.</summary>
    public enum CommType { Null, Tcp, Serial }

    /// <summary>How did operation turn out?</summary>
    public enum OpStatus { Success, Timeout, Error }

    /// <summary>Key modifiers</summary>
    public enum KeyMod { Ctrl, Alt, Shift, CtrlShift }

    /// <summary>ConsoleColor variation with None.</summary>
    public enum ConsoleColorEx
    {
        None = -1,
        Black = ConsoleColor.Black,
        DarkBlue = ConsoleColor.DarkBlue,
        DarkGreen = ConsoleColor.DarkGreen,
        DarkCyan = ConsoleColor.DarkCyan,
        DarkRed = ConsoleColor.DarkRed,
        DarkMagenta = ConsoleColor.DarkMagenta,
        DarkYellow = ConsoleColor.DarkYellow,
        Gray = ConsoleColor.Gray,
        DarkGray = ConsoleColor.DarkGray,
        Blue = ConsoleColor.Blue,
        Green = ConsoleColor.Green,
        Cyan = ConsoleColor.Cyan,
        Red = ConsoleColor.Red,
        Magenta = ConsoleColor.Magenta,
        Yellow = ConsoleColor.Yellow,
        White = ConsoleColor.White
    }
    #endregion

    #region Types
    /// <summary>One keyboard event data.</summary>
    /// <param name="Text">The content</param>
    /// <param name="Modifiers">Maybe modifiers</param>
    record CliInput(string Text, ConsoleModifiers Modifiers);

    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Send data to the server.</summary>
        /// <param name="data">What to send</param>
        /// <returns>Tuple of (operation status, error message).</returns>
        (OpStatus stat, string msg) Send(string data);

        /// <summary>Receive data from the server.</summary>
        /// <returns>Tuple of (operation status, error message, success data).</returns>
        (OpStatus stat, string msg, string data) Receive();

        /// <summary>Reset comms, resource management.</summary>
        void Reset();
    }
    #endregion

    public class Utils
    {
        /// <summary>For timing measurements.</summary>
        public static double GetCurrentMsec()
        {
            return (double)(1000.0 * Stopwatch.GetTimestamp() / Stopwatch.Frequency);
        }

        public static string BytesToString(byte[] buff)
        {
            var s = Encoding.Default.GetString(buff); // Use UTF8?
            return s;
        }

        public static string BytesToStringReadable(byte[] buff)
        {
            List<string> list = [];
            buff.ForEach(c => { list.Add(c.IsReadable() ? ((char)c).ToString() : $"<{c:X}>"); });
            return string.Join("", list);
        }

        public static byte[] StringToBytes(string s)
        {
            // Valid strings are always convertible.
            var buff = Encoding.Default.GetBytes(s);
            return buff;
        }
    }
}
