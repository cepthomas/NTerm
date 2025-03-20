using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace NTerm
{
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

    /// <summary>Supported communication flavors.</summary>
    public enum CommMode
    {
        /// <summary>Each command expects a response</summary>
        CmdResp,
        /// <summary>Check periodically for server msgs</summary>
        Poll
    }

    /// <summary>How did operation turn out?</summary>
    public enum OpStatus
    {
        /// <summary>OK</summary>
        Success,
        /// <summary></summary>
        Timeout,
        /// <summary>Poll with no answer</summary>
        NoResp,
        /// <summary>It's dead jim</summary>
        Error
    }

    /// <summary></summary>
    public enum ColorMode
    {
        /// <summary>Black and white</summary>
        None,
        /// <summary>Standard ANSI colot codes</summary>
        Ansi,
        /// <summary>Simple text matching</summary>
        Match
    }

    /// <summary>One keyboard event data.</summary>
    /// <param name="Text">The content</param>
    /// <param name="Ctrl">Control key is pressed</param>
    /// <param name="Alt">Alt key is pressed</param>
    record CliInput(string Text, bool Ctrl = false, bool Alt = false);

    /// <summary>Spec for one phrase using ColorMode.Match.</summary>
    /// <param name="Text"></param>
    /// <param name="WholeWord">Match whole word</param>
    /// <param name="WholeLine">Color whole line or just word</param>
    /// <param name="ForeColor">Optional color</param>
    /// <param name="BackColor">Optional color</param>
    public record Matcher(string Text, bool WholeWord, bool WholeLine, Color? ForeColor = null, Color? BackColor = null);

    /// <summary>Comm type abstraction.</summary>
    interface IComm : IDisposable
    {
        /// <summary>Alternate stream for debugging purposes.</summary>
        Stream? AltStream { get; set; }

        /// <summary>Initialize the comm device.</summary>
        /// <param name="config">Setup info.</param>
        /// <returns>Operation status, maybe error string.</returns>
        (OpStatus stat, string msg) Init(Config config);

        /// <summary>Send bytes to the server.</summary>
        /// <param name="tx">What to send, empty indicates a poll request</param>
        /// <returns>Operation status, response.</returns>
        (OpStatus stat, byte[] rx) Send(byte[] tx);

        /// <summary>Clean up.</summary>
        public new void Dispose();
    }

    public class Utils
    {
        public static string BytesToString(byte[] buff) // TODOF Put in nbot.
        {
            //var s = Encoding.Default.GetString(buff); // Use UTF8?

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
