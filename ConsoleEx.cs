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


        //////// all real Console ////////

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

        ///// Cursor ops - on buffer C/R
        // bool CursorVisible
        // int CursorLeft
        // int CursorSize
        // int CursorTop
        // (int Left, int Top) GetCursorPosition()
        // void SetCursorPosition(int left, int top)

        ///// Buffer ops - on buffer C/R
        // void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop)
        // void MoveBufferArea(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, int targetLeft, int targetTop, char sourceChar, ConsoleColor sourceForeColor, ConsoleColor sourceBackColor)
        // void SetBufferSize(int width, int height)
        // void SetWindowPosition(int left, int top)
        // void SetWindowSize(int width, int height)

        ///// Lots of Write() and WriteLine() overloads - implemented as needed.
    }



    /////////////////////////////////////////////////////////////////////////////////////
    public class RealConsole : IConsole
    {
        public bool KeyAvailable { get => Console.KeyAvailable; }
        public string Title { get => Console.Title; set => Console.Title = value; }
        public int WindowHeight { get => Console.WindowHeight; set => Console.WindowHeight = value; }
        public int WindowLeft { get => Console.WindowLeft; set => Console.WindowLeft = value; }
        public int WindowTop { get => Console.WindowTop; set => Console.WindowTop = value; }
        public int WindowWidth { get => Console.WindowWidth; set => Console.WindowWidth = value; }
        public ConsoleColor BackgroundColor { get => Console.BackgroundColor; set => Console.BackgroundColor = value; }
        public ConsoleColor ForegroundColor { get => Console.ForegroundColor; set => Console.ForegroundColor = value; }

        public string? ReadLine() { return Console.ReadLine(); }
        public ConsoleKeyInfo ReadKey(bool intercept) { return Console.ReadKey(intercept); }
        public void Write(string text) { Console.Write(text); }
        public void WriteLine(string text) { Console.WriteLine(text); }
        public ConsoleKeyInfo ReadKey() { return Console.ReadKey(); }
        public void Clear() { Console.Clear(); }
        public void ResetColor() { Console.ResetColor(); }
        public void WriteLine() { Console.WriteLine(); }
        public int Read() { return Console.Read(); }
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
