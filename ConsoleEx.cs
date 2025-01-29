using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


namespace NTerm
{
    public class ConsoleEx : IDisposable
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        bool _forceNewline = false;
        Encoding _encoding = Encoding.UTF8;
        bool _convertToPrintable = false;
        bool _clearScreen = true;
        bool _noStatus = false;

        // dictionary of "special" keys with the corresponding string to send out when they are pressed
        Dictionary<ConsoleKey, string> _specialKeys = new Dictionary<ConsoleKey, string>
        {
            { ConsoleKey.UpArrow, "\x1B[A" },
            { ConsoleKey.DownArrow, "\x1B[B" },
            { ConsoleKey.RightArrow, "\x1B[C" },
            { ConsoleKey.LeftArrow, "\x1B[D" },
            { ConsoleKey.Home, "\x1B[H" },
            { ConsoleKey.End, "\x1B[F" },
            { ConsoleKey.Insert, "\x1B[2~" },
            { ConsoleKey.Delete, "\x1B[3~" },
            { ConsoleKey.PageUp, "\x1B[5~" },
            { ConsoleKey.PageDown, "\x1B[6~" },
            { ConsoleKey.F1, "\x1B[11~" },
            { ConsoleKey.F2, "\x1B[12~" },
            { ConsoleKey.F3, "\x1B[13~" },
            { ConsoleKey.F4, "\x1B[14~" },
            { ConsoleKey.F5, "\x1B[15~" },
            { ConsoleKey.F6, "\x1B[17~" },
            { ConsoleKey.F7, "\x1B[18~" },
            { ConsoleKey.F8, "\x1B[19~" },
            { ConsoleKey.F9, "\x1B[20~" },
            { ConsoleKey.F10, "\x1B[21~" },
            { ConsoleKey.F11, "\x1B[23~" },
            { ConsoleKey.F12, "\x1B[24~" }
        };

        public ConsoleEx() //string[] args)
        {
            // attempt to enable virtual terminal escape sequence processing
            if (!_convertToPrintable)
            {
                try
                {
                    var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
                    var err = GetLastError();

                    //If the function fails, the return value is INVALID_HANDLE_VALUE.To get extended error information,
                    //call GetLastError.
                    //If an application does not have associated standard handles, such as a service running on an
                    //interactive desktop, and has not redirected them, the return value is NULL.


                    //== INVALID_HANDLE
                    GetConsoleMode(iStdOut, out uint outConsoleMode);
                    outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                    SetConsoleMode(iStdOut, outConsoleMode);
                }
                catch
                {
                    // if the above fails, it doesn't really matter - it just means escape sequences won't process nicely
                }
            }

            Console.OutputEncoding = _encoding;

            // set up keyboard input for program control / relay to serial port
            ConsoleKeyInfo keyInfo = new ConsoleKeyInfo();
            Console.TreatControlCAsInput = true; // we need to use CTRL-C to activate the REPL in CircuitPython, so it can't be used to exit the application

            // this is where data read from the serial port will be temporarily stored
            string received = string.Empty;

            //main loop - keep this up until user presses CTRL-X or an exception takes us down
            do
            {
                // first things first, check for (and respect) a request to exit the program via CTRL-X
                if (Console.KeyAvailable)
                {
                    keyInfo = Console.ReadKey(intercept: true);

                    if ((keyInfo.Key == ConsoleKey.H) && (keyInfo.Modifiers == ConsoleModifiers.Control))
                    {
                        ShowHelp();
                    }

                    if ((keyInfo.Key == ConsoleKey.X) && (keyInfo.Modifiers == ConsoleModifiers.Control))
                    {
                        Output("\n<<< SimplySerial session terminated via CTRL-X >>>");
                        ExitProgram(silent: true);
                    }

                }

                // this is the core functionality - loop while the serial port is open
                while (_serialPort.IsOpen)
                {
                    try
                    {
                        // process keypresses for transmission through the serial port
                        if (Console.KeyAvailable)
                        {
                            // determine what key is pressed (including modifiers)
                            keyInfo = Console.ReadKey(intercept: true);

                            // exit the program if CTRL-X was pressed
                            if ((keyInfo.Key == ConsoleKey.X) && (keyInfo.Modifiers == ConsoleModifiers.Control))
                            {
                                Output("\n<<< SimplySerial session terminated via CTRL-X >>>");
                                throw new();
                            }

                            // check for keys that require specia           l processing (cursor keys, etc.)
                            else if (_specialKeys.ContainsKey(keyInfo.Key))
                                _serialPort.Write(_specialKeys[keyInfo.Key]);

                            // everything else just gets sent right on through
                            else
                                _serialPort.Write(Convert.ToString(keyInfo.KeyChar));
                        }

                        // process data coming in from the serial port
                        received = _serialPort.ReadExisting();

                        // if anything was received, process it
                        if (received.Length > 0)
                        {
                            // if we're trying to filter out title/status updates in received data, try to ensure we've got the whole string
                            if (_noStatus && received.Contains("\x1b"))
                            {
                                Thread.Sleep(100);
                                received += _serialPort.ReadExisting();
                            }

                            if (_forceNewline)
                                received = received.Replace("\r", "\n");

                            // write what was received to console
                            Output(received, force: true, newline: false);
                            start = DateTime.Now;
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }
            } while (_autoConnect > AutoConnect.NONE);

            // if we get to this point, we should be exiting gracefully
            throw new("<<< SimplySerial session terminated >>>");
        }


        public void Dispose()
        {
            // _serialPort?.Close();
            // _serialPort?.Dispose();
            // _serialPort = null;
        }

        /// <summary>
        /// Writes messages using Console.WriteLine() as long as the 'Quiet' option hasn't been enabled
        /// </summary>
        /// <param name="message">Message to output (assuming 'Quiet' is false)</param>
        void Output(string message, bool force = false, bool newline = true, bool flush = false)
        {
            if (!_quiet || force)
            {
                if (newline)
                    message += "\n";

                if (message.Length > 0)
                {
                    if (_noStatus)
                    {
                        Regex r = new Regex(@"\x1b\][02];.*\x1b\\");
                        message = r.Replace(message, string.Empty);
                    }

                    if (_convertToPrintable)
                    {
                        string newMessage = "";
                        foreach (byte c in message)
                        {
                            if ((c > 31 && c < 128) || (c == 8) || (c == 9) || (c == 10) || (c == 13))
                                newMessage += (char)c;
                            else
                                newMessage += $"[{c:X2}]";
                        }
                        message = newMessage;
                    }
                    Console.Write(message);
                }
            }
        }
    }
}
