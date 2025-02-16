using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    /// <summary>Supported flavors.</summary>
    public enum CommType { Null, Tcp, Serial }

    /// <summary>How did we do?</summary>
    public enum OpStatus { Success, Timeout, Error, ConfigError }

    /// <summary></summary>
    public enum ColorMode { None, Ansi, Match }

    /// <summary>Internal version of core types.</summary>
    enum Modifier { None, Ctrl, Alt }

    /// <summary>Internal data container.</summary>
    record CliInput(Modifier Mod, string Text);



    /// <summary>Spec for one match.</summary>
    /// <param name="Text"></param>
    /// <param name="WholeWord"></param>
    /// <param name="WholeLine"></param>
    /// <param name="ForeColor"></param>
    /// <param name="BackColor"></param>
    public record Matcher(string Text, bool WholeWord, bool WholeLine, Color? ForeColor, Color? BackColor);

    /// <summary>Comm type abstraction.</summary>
    public interface IComm : IDisposable
    {
        /// <summary>Server must connect or reply to commands in msec.</summary>
        int ResponseTime { get; set; }

        /// <summary>R/W buffer size.</summary>
        int BufferSize { get; set; }

        /// <summary>The text if Success otherwise error message.</summary>
        string Response { get; }

        /// <summary>Initialize the comm device.</summary>
        /// <param name="args">Setup info.</param>
        /// <returns>Operation status.</returns>
        OpStatus Init(string args);

        /// <summary>Send a message to the server.</summary>
        /// <param name="msg">What to send.</param>
        /// <returns>Operation status.</returns>
        OpStatus Send(string msg);

        /// <summary>Clean up.</summary>
        public new void Dispose();
    }


    /// <summary>Serial port abstraction. Members defined in MS docs.</summary>
    public interface ISerialPort : IDisposable
    {
        string PortName { get; set; }
        int BaudRate { get; set; }
        Parity Parity { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }
        int ReadBufferSize { get; set; }
        int WriteBufferSize { get; set; }
        int ReadTimeout { get; set; }
        int WriteTimeout { get; set; }
        bool IsOpen { get; }
        Stream BaseStream { get; }
        void Open();
        void Close();
    }
}
