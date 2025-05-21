using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    #region Exceptions
    /// <summary>Lua script syntax error.</summary>
    public class SyntaxException(string message) : Exception(message) { }
    #endregion

    /// <summary>Supported flavors.</summary>
    public enum CommType
    {
        /// <summary>Default comm that echoes sent string.</summary>
        Null,
        /// <summary>Standard TCP</summary>
        Tcp,
        /// <summary>Standard serial port</summary>
        Serial,
    }

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

    /// <summary>How did operation turn out?</summary>
    public enum OpStatus
    {
        /// <summary>OK</summary>
        Success,
        /// <summary>Not connected</summary>
        Timeout,
// /// <summary>Connected but not responding</summary>
// Ignoring,
        /// <summary>It's dead jim</summary>
        Error
    }

    /// <summary>Key modifiers</summary>
    public enum KeyMod { Ctrl, Alt, Shift, CtrlShift }

    /// <summary>One keyboard event data.</summary>
    /// <param name="Text">The content</param>
    /// <param name="Modifiers">Maybe modifiers</param>
    record CliInput(string Text, ConsoleModifiers Modifiers);

    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Alternate stream for debugging purposes. TODO</summary>
        Stream? AltStream { get; set; }

        /// <summary>Reset comms, resource management.</summary>
        void Reset();

        /// <summary>Send data to the server.</summary>
        /// <param name="data">What to send</param>
        /// <returns>Tuple of (operation status, error message).</returns>
        (OpStatus stat, string msg) Send(string data);

        /// <summary>Receive data from the server.</summary>
        /// <returns>Tuple of (operation status, error message, success data).</returns>
        (OpStatus stat, string msg, string data) Receive();
    }

    public class Utils
    {
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

    //#region Console abstraction to support testing
    //public interface IConsole
    //{
    //    bool KeyAvailable { get; }
    //    bool CursorVisible { get; set; }
    //    string Title { get; set; }
    //    int BufferWidth { get; set; }
    //    void Write(string text);
    //    void WriteLine(string text);
    //    string? ReadLine();
    //    ConsoleKeyInfo ReadKey(bool intercept);
    //    (int left, int top) GetCursorPosition();
    //    void SetCursorPosition(int left, int top);
    //}
    //#endregion

}
