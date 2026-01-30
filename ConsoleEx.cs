using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;


namespace Ephemera.NBagOfTricks // TODO1 find a home for this - prob NBOT.
{
    /// <summary>
    /// Interface for consoles. Subset of System.Console.
    /// </summary>
    public interface IConsole
    {
        #region Properties
        bool KeyAvailable { get; }
        string Title { get; set; }
        int WindowHeight { get; set; }
        int WindowLeft { get; set; }
        int WindowTop { get; set; }
        int WindowWidth { get; set; }
        ConsoleColor BackgroundColor { get; set; }
        ConsoleColor ForegroundColor { get; set; }
        #endregion

        #region Functions
        void Write(string text);
        void WriteLine(string text);
        string? ReadLine();
        ConsoleKeyInfo ReadKey(bool intercept);
        ConsoleKeyInfo ReadKey();
        void Clear();
        void ResetColor();
        void WriteLine();
        int Read();
        #endregion

        #region All other real Console members - not implemented
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

        ///// Lots of Write() and WriteLine() overloads - implement as needed.
        #endregion
    }

    /// <summary>
    /// The real console. Mainly pass-through for interface members.
    /// </summary>
    public class RealConsole : IConsole
    {
        #region Properties
        public bool KeyAvailable { get => Console.KeyAvailable; }
        public string Title { get => Console.Title; set => Console.Title = value; }
        public ConsoleColor BackgroundColor { get => Console.BackgroundColor; set => Console.BackgroundColor = value; }
        public ConsoleColor ForegroundColor { get => Console.ForegroundColor; set => Console.ForegroundColor = value; }

        // Setting window row/column doesn't work in .NET so take a dive into win32.
        public int WindowLeft
        {
            get { var r = GetRect(); return r.X; }
            set { var r = GetRect(); Move(value, r.Y, r.Width, r.Height); }
        }

        public int WindowTop
        {
            get { var r = GetRect(); return r.Top; }
            set { var r = GetRect(); Move(r.X, value, r.Width, r.Height); }
        }

        public int WindowWidth
        {
            get { var r = GetRect(); return r.Width; }
            set { var r = GetRect(); Move(r.X, r.Y, value, r.Height); }
        }

        public int WindowHeight
        {
            get { var r = GetRect(); return r.Height; }
            set { var r = GetRect(); Move(r.X, r.Y, r.Width, value); }
        }
        #endregion

        #region Functions
        public string? ReadLine() { return Console.ReadLine(); }
        public ConsoleKeyInfo ReadKey(bool intercept) { return Console.ReadKey(intercept); }
        public void Write(string text) { Console.Write(text); }
        public void WriteLine(string text) { Console.WriteLine(text); }
        public ConsoleKeyInfo ReadKey() { return Console.ReadKey(); }
        public void Clear() { Console.Clear(); }
        public void ResetColor() { Console.ResetColor(); }
        public void WriteLine() { Console.WriteLine(); }
        public int Read() { return Console.Read(); }
        #endregion

        #region Native win32 functions
        struct RectNative
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        const int SW_MAXIMIZE = 3;

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RectNative lpRect);

        [DllImport("user32.dll")]
        static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

        static void Move(int x, int y, int width, int height)
        {
            IntPtr hnd = GetForegroundWindow();
            MoveWindow(hnd, x, y, width, height, true);
        }

        static Rectangle GetRect()
        {
            IntPtr hnd = GetForegroundWindow();
            GetWindowRect(hnd, out RectNative nrect);
            return new Rectangle(nrect.Left, nrect.Top, nrect.Right - nrect.Left, nrect.Bottom - nrect.Top);
        }
        #endregion
    }

    /// <summary>
    /// A mock Console suitable for testing by simulating/capturing input and input.
    /// </summary>
    public class MockConsole : IConsole
    {
        #region Fields
        readonly StringBuilder _capture = new();
        #endregion

        #region Internals
        public List<string> Capture { get { return StringUtils.SplitByTokens(_capture.ToString(), Environment.NewLine); } }
        public string NextReadLine { get; set; } = "";
        #endregion

        #region IConsole implementation - Properties
        public bool KeyAvailable { get => NextReadLine.Length > 0; }

        public string Title { get; set; } = "";

        public int WindowHeight { get; set; } = 40;

        public int WindowLeft { get; set; } = 100;

        public int WindowTop { get; set; } = 50;

        public int WindowWidth { get; set; } = 100;

        public ConsoleColor BackgroundColor { get; set; }

        public ConsoleColor ForegroundColor { get; set; }
        #endregion

        #region IConsole implementation - Functions
        public void ResetColor()
        {
            BackgroundColor = ConsoleColor.Black;
            ForegroundColor = ConsoleColor.White;
        }

        public void Write(string text) => _capture.Append(text);

        public void WriteLine(string text) => _capture.Append(text + Environment.NewLine);

        public void WriteLine() => _capture.Append(Environment.NewLine);

        public void Clear() => _capture.Clear();

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

        // The pressed key is optionally displayed in the console window.
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
                // TODO block until new key.
                throw new InvalidOperationException();
            }
        }

        public ConsoleKeyInfo ReadKey() => ReadKey(false);

        public int Read()
        {
            if (KeyAvailable)
            {
                var key = NextReadLine[0];
                NextReadLine = NextReadLine.Substring(1);
                return (int)key;
            }
            else
            {
                return -1;
            }
        }
        #endregion
    }
}
