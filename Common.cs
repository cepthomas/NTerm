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

    ///// <summary>Supported communication flavors.</summary>
    //public enum CommMode
    //{
    //    /// <summary>Each command expects a response</summary>
    //    CmdResp,
    //    /// <summary>Check periodically for server msgs</summary>
    //    Poll
    //}

    /// <summary>How did operation turn out?</summary>
    public enum OpStatus
    {
        /// <summary>OK</summary>
        Success,
        /// <summary>Not connected</summary>
        Timeout,
        /// <summary>Connected but not responding</summary>
        Ignoring,
        ///// <summary>Poll with no answer</summary>
        //NoResp,
        /// <summary>It's dead jim</summary>
        Error
    }

    // /// <summary></summary>
    // public enum ColorMode
    // {
    //     /// <summary>Black and white</summary>
    //     None,
    //     /// <summary>Standard ANSI colot codes</summary>
    //     Ansi,
    //     /// <summary>Simple text matching</summary>
    //     Match
    // }

    /// <summary>Key modifiers</summary>
    public enum KeyMod { Ctrl, Alt, Shift, CtrlShift }

    /// <summary>One keyboard event data.</summary>
    /// <param name="Text">The content</param>
    /// <param name="Modifiers">Maybe modifiers</param>
    record CliInput(string Text, ConsoleModifiers Modifiers);

    ///// <summary>One keyboard event data.</summary>
    ///// <param name="Text">The content</param>
    ///// <param name="Ctrl">Control key is pressed</param>
    ///// <param name="Alt">Alt key is pressed</param>
    //record CliInput(string Text, bool Ctrl, bool Alt, bool Shift);

    /// <summary>Spec for one phrase using line match.</summary>
    /// <param name="Text"></param>
    /// <param name="WholeWord">Match whole word</param>
    /// <param name="WholeLine">Color whole line or just word</param>
    /// <param name="ForeColor">Optional color</param>
    /// <param name="BackColor">Optional color</param>
    //public record Matcher(string Text, bool WholeWord, bool WholeLine, ConsoleColor? ForeColor = null, ConsoleColor? BackColor = null);

    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Alternate stream for debugging purposes. TODO</summary>
        Stream? AltStream { get; set; }

        /// <summary>Initialize the comm device.</summary>
        /// <param name="config">Setup info.</param>
        /// <returns>Tuple of (operation status, error message).</returns>
      //  (OpStatus stat, string msg) Init(Config config);

        /// <summary>Reset comms, resource management.</summary>
        void Reset();

        /// <summary>Send data to the server.</summary>
        /// <param name="data">What to send</param>
        /// <returns>Tuple of (operation status, error message).</returns>
        (OpStatus stat, string msg) Send(string data);

        /// <summary>Receive data from the server.</summary>
        /// <returns>Tuple of (operation status, error message, success data).</returns>
        (OpStatus stat, string msg, string data) Receive();

        /// <summary>Clean up.</summary>
//        public new void Dispose();
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
