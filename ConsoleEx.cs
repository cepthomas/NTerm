using System;
using System.Collections.Generic;
using System.Text;


namespace Ephemera.NBagOfTricks // Nebulua_TODO1 // find a home for this.
{
    public interface IConsole
    {
        bool KeyAvailable { get; }
        string Title { get; set; }
        void Write(string text);
        void WriteLine(string text);
        string? ReadLine();
        ConsoleKeyInfo ReadKey(bool intercept);

        ///// added /////////////////////////////////////////////////////////
        int WindowHeight { get; set; }
        int WindowLeft { get; set; }
        int WindowTop { get; set; }
        int WindowWidth { get; set; }
        ConsoleColor BackgroundColor { get; set; }
        ConsoleColor ForegroundColor { get; set; }
        ConsoleKeyInfo ReadKey();
        void Clear();
        void ResetColor();
        void WriteLine();
        // Reads the next character from the input stream. The returned value is -1 if no further characters are available.
        int Read();

        ///// new
        // w.Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
        // w.Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);
        // _settings.FormGeometry = new Rectangle(f.X, f.Y, f.Width, f.Height);



        //////// all real ////////

        ///// Basics
        // bool CapsLock
        // bool NumberLock
        // bool TreatControlCAsInput
        // Encoding InputEncoding
        // Encoding OutputEncoding
        // int BufferHeight
        // int BufferWidth
        // int LargestWindowHeight
        // int LargestWindowWidth
        // void Beep()
        // void Beep(int frequency, int duration)
        // event ConsoleCancelEventHandler? CancelKeyPress

        ///// STD streams
        // TextReader In
        // TextWriter Error
        // TextWriter Out
        // Stream OpenStandardError()
        // Stream OpenStandardError(int bufferSize)
        // Stream OpenStandardInput()
        // Stream OpenStandardInput(int bufferSize)
        // Stream OpenStandardOutput()
        // Stream OpenStandardOutput(int bufferSize)
        // void SetError(TextWriter newError)
        // void SetIn(TextReader newIn)
        // void SetOut(TextWriter newOut)
        // bool IsErrorRedirected
        // bool IsInputRedirected
        // bool IsOutputRedirected

        ///// Cursor ops - in buffer C/R
        // bool CursorVisible
        // int CursorLeft
        // int CursorSize
        // int CursorTop
        // (int Left, int Top) GetCursorPosition()
        // void SetCursorPosition(int left, int top)


        ///// Internals
        // void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
        // void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
        // void SetBufferSize(int width, int height)
        // Window ops relative to buffer:
        // void SetWindowPosition(int left, int top)
        // void SetWindowSize(int width, int height)


        ///// Write() overloads
        // void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
        // void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
        // void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
        // void Write([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[]? arg)
        // void Write(bool value)
        // void Write(char value)
        // void Write(char[] buffer, int index, int count)
        // void Write(char[]? buffer)
        // void Write(decimal value)
        // void Write(double value)
        // void Write(float value)
        // void Write(int value)
        // void Write(long value)
        // void Write(object? value)
        // void Write(string? value)
        // void Write(uint value)
        // void Write(ulong value)

        ///// WriteLine() overloads
        // void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0)
        // void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1)
        // void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, object? arg0, object? arg1, object? arg2)
        // void WriteLine([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string format, params object?[]? arg)
        // void WriteLine(bool value)
        // void WriteLine(char value)
        // void WriteLine(char[] buffer, int index, int count)
        // void WriteLine(char[]? buffer)
        // void WriteLine(decimal value)
        // void WriteLine(double value)
        // void WriteLine(float value)
        // void WriteLine(int value)
        // void WriteLine(long value)
        // void WriteLine(object? value)
        // void WriteLine(string? value)
        // void WriteLine(uint value)
        // void WriteLine(ulong value)
    }



    /////////////////////////////////////////////////////////////////////////////////////
    public class RealConsole : IConsole
    {
        public bool KeyAvailable { get => Console.KeyAvailable; }
        public string Title { get => Console.Title; set => Console.Title = value; }
        int IConsole.WindowHeight { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        int IConsole.WindowLeft { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        int IConsole.WindowTop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        int IConsole.WindowWidth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        ConsoleColor IConsole.BackgroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        ConsoleColor IConsole.ForegroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string? ReadLine() { return Console.ReadLine(); }
        public ConsoleKeyInfo ReadKey(bool intercept) { return Console.ReadKey(intercept); }
        public void Write(string text) { Console.Write(text); }
        public void WriteLine(string text) { Console.WriteLine(text); }

        ConsoleKeyInfo IConsole.ReadKey()
        {
            throw new NotImplementedException();
        }

        void IConsole.Clear()
        {
            throw new NotImplementedException();
        }

        void IConsole.ResetColor()
        {
            throw new NotImplementedException();
        }

        void IConsole.WriteLine()
        {
            throw new NotImplementedException();
        }

        int IConsole.Read()
        {
            throw new NotImplementedException();
        }

        public RealConsole()
        {
        }
    }


    //////////////////////////////////////////////////////////////////////////////////////////
    public class MockConsole : IConsole
    {
        #region Fields
        readonly StringBuilder _capture = new();
        #endregion

        #region Internals
        //        public List<string> Capture { get { return StringUtils.SplitByTokens(_capture.ToString(), Environment.NewLine); } }
        public string NextReadLine { get; set; } = "";
        public void Reset() => _capture.Clear();
        #endregion

        #region IConsole implementation
        public bool KeyAvailable { get => NextReadLine.Length > 0; }
        public string Title { get; set; } = "";
        int IConsole.WindowHeight { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        int IConsole.WindowLeft { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        int IConsole.WindowTop { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        int IConsole.WindowWidth { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        ConsoleColor IConsole.BackgroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        ConsoleColor IConsole.ForegroundColor { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public string? ReadLine()
        {
            if (NextReadLine == "")
            {
                return null;
            }
            else
            {
                var ret = NextReadLine;
                NextReadLine = "";
                return ret;
            }
        }

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            if (KeyAvailable)
            {
                var key = NextReadLine[0];
                NextReadLine = NextReadLine.Substring(1);
                return new ConsoleKeyInfo(key, (ConsoleKey)key, false, false, false);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Write(string text) => _capture.Append(text);

        public void WriteLine(string text) => _capture.Append(text + Environment.NewLine);

        ConsoleKeyInfo IConsole.ReadKey()
        {
            throw new NotImplementedException();
        }

        void IConsole.Clear()
        {
            throw new NotImplementedException();
        }

        void IConsole.ResetColor()
        {
            throw new NotImplementedException();
        }

        void IConsole.WriteLine()
        {
            throw new NotImplementedException();
        }

        int IConsole.Read()
        {
            throw new NotImplementedException();
        }
        #endregion
    }



    ///////////////////////////////// test stuff ////////////////////////////////////
    public class CliHost : IDisposable
    {
        #region Fields
        /// <summary>App logger.</summary>
        //readonly Logger _logger = LogManager.CreateLogger("CLI");

        ///// <summary>Common functionality.</summary>
        //readonly HostCore _hostCore = new();

        /// <summary>Resource management.</summary>
        bool _disposed = false;


        /// <summary>CLI.</summary>
        readonly IConsole _console;

        /// <summary>CLI prompt.</summary>
        readonly string _prompt = ">";
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits stuff.
        /// </summary>
        /// <param name="scriptFn">Cli version requires cl script name.</param>
        /// <param name="tin">Stream in</param>
        /// <param name="tout">Stream out</param>
        public CliHost(string scriptFn, IConsole console)
        {
            _console = console;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
        #endregion
    }

    /// <summary>Test the simpler functions.</summary>
    public class CLI_PNUT
    {
        public void RunSuite()
        {
            //bool bret;

            MockConsole console = new();
           // var cli = new Cli("none", console);

            string prompt = ">";

            console.Reset();
            console.NextReadLine = "bbbbb";
            //bret = cli.DoCommand();
            //UT_EQUAL(console.Capture.Count, 2);
            //UT_EQUAL(console.Capture[0], $"Invalid command");
            //UT_EQUAL(console.Capture[1], prompt);
        }
    }
}
