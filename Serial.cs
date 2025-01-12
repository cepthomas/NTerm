//using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
//using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;


namespace NTerm
{
    public enum AutoConnect { NONE, ONE, ANY };

    public class Vendor
    {
        public string vid = "----";
        public string make = "VID";
    }

    /// <summary>
    /// Custom structure containing the name, VID, PID and description of a serial (COM) port
    /// Modified from the example written by Kamil Górski (freakone) available at
    /// http://blog.gorski.pm/serial-port-details-in-c-sharp
    /// https://github.com/freakone/serial-reader
    /// </summary>
    public class ComPort // custom struct with our desired values
    {
        public string name;
        public int num = -1;
        public string vid = "----";
        public string pid = "----";
        public string description;
        public string busDescription;
        //public Board board;
       // public bool isCircuitPython = false;
    }

    public class SimplySerial : IDisposable //IProtocol
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

        //static string appFolder = AppDomain.CurrentDomain.BaseDirectory;
        //static BoardData boardData;

        List<ComPort> _availablePorts = new List<ComPort>();
        SerialPort? _serialPort = null;

        // default comspec values and application settings set here will be overridden by values passed through command-line arguments
        bool _quiet = false;
        AutoConnect _autoConnect = AutoConnect.ONE;
        ComPort _port = new ComPort();
        int _baud = -1;
        Parity _parity = Parity.None;
        int _dataBits = 8;
        StopBits _stopBits = StopBits.One;
        //bool logging = false;
        //FileMode logMode = FileMode.Create;
        //string logFile = string.Empty;
        //string logData = string.Empty;
        //int bufferSize = 102400;
        //DateTime lastFlush = DateTime.Now;
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


        SimplySerial(string[] args)
        {
            // load and parse data in boards.json
            //LoadBoards();

            // process all command-line arguments
            //ProcessArguments(args);

            // attempt to enable virtual terminal escape sequence processing
            if (!_convertToPrintable)
            {
                try
                {
                    var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
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
                    if ((keyInfo.Key == ConsoleKey.X) && (keyInfo.Modifiers == ConsoleModifiers.Control))
                    {
                        Output("\n<<< SimplySerial session terminated via CTRL-X >>>");
 //>>>                       ExitProgram(silent: true);
                    }
                }

                // get a list of available ports
                _availablePorts = GetSerialPorts().OrderBy(p => p.num).ToList();

                // if no port was specified/selected, pick one automatically
                if (_port.name == string.Empty)
                {
                    // if there are com ports available, pick one
                    if (_availablePorts.Count() >= 1)
                    {
                        // first, try to default to something that we assume is running CircuitPython
                        //SimplySerial._port = _availablePorts.Find(p => p.isCircuitPython == true);

                        // if that doesn't work out, just default to the first available COM port
                        if (_port == null)
                            _port = _availablePorts[0];
                    }

                    // if there are no com ports available, exit or try again depending on autoconnect setting 
                    else
                    {
                        if (_autoConnect == AutoConnect.NONE)
                            throw new("No COM ports detected.");
                        else
                            continue;
                    }
                }

                // if a specific port has been selected, try to match it with one that actually exists
                else
                {
                    bool portMatched = false;

                    foreach (ComPort p in _availablePorts)
                    {
                        if (p.name == _port.name)
                        {
                            portMatched = true;
                            _port = p;
                            break;
                        }
                    }

                    // if the specified port is not available, exit or try again depending on autoconnect setting
                    if (!portMatched)
                    {
                        if (_autoConnect == AutoConnect.NONE)
                            throw new($"Invalid port specified <" + _port.name + ">");
                        else
                            continue;
                    }
                }

                // if we get this far, it should be safe to set up the specified/selected serial port
                _serialPort = new SerialPort(_port.name)
                {
                    Handshake = Handshake.None, // we don't need to support any handshaking at this point 
                    ReadTimeout = 1, // minimal timeout - we don't want to wait forever for data that may not be coming!
                    WriteTimeout = 250, // small delay - if we go too small on this it causes System.IO semaphore timeout exceptions
                    DtrEnable = true, // without this we don't ever receive any data
                    RtsEnable = true, // without this we don't ever receive any data
                    Encoding = _encoding
                };

                // attempt to set the baud rate, fail if the specified value is not supported by the hardware
                try
                {
                    if (_baud < 0)
                    {
                        _baud = 115200;
                        //if (_port.isCircuitPython)
                        //    _baud = 115200;
                        //else
                        //    _baud = 9600;
                    }

                    _serialPort.BaudRate = _baud;
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new($"Invalid port specified <" + _baud + ") is not supported.");
                }

                // set other port parameters (which have already been validated)
                _serialPort.Parity = _parity;
                _serialPort.DataBits = _dataBits;
                _serialPort.StopBits = _stopBits;

                // attempt to open the serial port, deal with failures
                try
                {
                    _serialPort.Open();
                }
                catch (Exception e)
                {
                    // if auto-connect is disabled than any exception should result in program termination
                    if (_autoConnect == AutoConnect.NONE)
                    {
                        if (e is UnauthorizedAccessException)
                            throw new((e.GetType() + " occurred while attempting to open " + _port.name + ".  Is this port already in use in another application?"));
                        else
                            throw new((e.GetType() + " occurred while attempting to open " + _port.name + "."));
                    }

                    // if auto-connect is enabled, prepare to try again
                    _serialPort.Dispose();
                    Thread.Sleep(1000); // putting a delay here to avoid gobbling tons of resources thruogh constant high-speed re-connect attempts
                    continue;
                }

                //Console.Title = $"{port.name}: {port.board.make} {port.board.model}";

                // if we get this far, clear the screen and send the connection message if not in 'quiet' mode
                if (_clearScreen)
                {
                    Console.Clear();
                }
                else
                {
                    Output("");
                }
                //Output(string.Format("<<< SimplySerial v{0} connected via {1} >>>\n" +
                //    "Settings  : {2} baud, {3} parity, {4} data bits, {5} stop bit{6}, {7} encoding, auto-connect {8}\n" +
                //    "Device    : {9} {10}{11}\n{12}" +
                //    "---\n\nUse CTRL-X to exit.\n",
                //    "version",
                //    _port.name,
                //    _baud,
                //    (_parity == Parity.None) ? "no" : (_parity.ToString()).ToLower(),
                //    _dataBits,
                //    (_stopBits == StopBits.None) ? "0" : (_stopBits == StopBits.One) ? "1" : (_stopBits == StopBits.OnePointFive) ? "1.5" : "2", (_stopBits == StopBits.One) ? "" : "s",
                //    (_encoding.ToString() == "System.Text.UTF8Encoding") ? "UTF-8" : (_convertToPrintable) ? "RAW" : "ASCII",
                //    (_autoConnect == AutoConnect.ONE) ? "on" : (_autoConnect == AutoConnect.ANY) ? "any" : "off",
                //    "port.board.make",
                //    "port.board.model",
                //    (_port.isCircuitPython) ? " (CircuitPython-capable)" : "",
                //    (logging == true) ? ($"Logfile   : {logFile} (Mode = " + ((logMode == FileMode.Create) ? "OVERWRITE" : "APPEND") + ")\n") : ""
                //), flush: true);

                //lastFlush = DateTime.Now;
                DateTime start = DateTime.Now;
                TimeSpan timeSinceRX = new TimeSpan();
                TimeSpan timeSinceFlush = new TimeSpan();

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

                            // check for keys that require special processing (cursor keys, etc.)
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
                            Thread.Sleep(1);

                        //if (logging)
                        //{
                        //    timeSinceRX = DateTime.Now - start;
                        //    timeSinceFlush = DateTime.Now - lastFlush;
                        //    if ((timeSinceRX.TotalSeconds >= 2) || (timeSinceFlush.TotalSeconds >= 10))
                        //    {
                        //        if (logData.Length > 0)
                        //            Output("", force: true, newline: false, flush: true);
                        //        start = DateTime.Now;
                        //        lastFlush = DateTime.Now;
                        //    }
                        //}

                        // if the serial port is unexpectedly closed, throw an exception
                        if (!_serialPort.IsOpen)
                            throw new IOException();
                    }
                    catch (Exception e)
                    {
                        if (_autoConnect == AutoConnect.NONE)
                            throw new((e.GetType() + " occurred while attempting to read/write to/from " + _port.name + "."));
                        else
                        {
                            Console.Title = $"{_port.name}: (disconnected)";
                            Output("\n<<< Communications Interrupted >>>\n");
                        }
                        try
                        {
                            _serialPort.Dispose();
                        }
                        catch
                        {
                            //nothing to do here, other than prevent execution from stopping if dispose() throws an exception
                        }
                        Thread.Sleep(2000); // sort-of arbitrary delay - should be long enough to read the "interrupted" message
                        if (_autoConnect == AutoConnect.ANY)
                        {
                            Console.Title = "SimplySerial: Searching...";
                            _port.name = string.Empty;
                            Output("<<< Attemping to connect to any available COM port.  Use CTRL-X to cancel >>>");
                        }
                        else if (_autoConnect == AutoConnect.ONE)
                        {
                            Console.Title = $"{_port.name}: Searching...";
                            Output("<<< Attempting to re-connect to " + _port.name + ". Use CTRL-X to cancel >>>");
                        }
                        break;
                    }
                }
            } while (_autoConnect > AutoConnect.NONE);

            // if we get to this point, we should be exiting gracefully
            throw new("<<< SimplySerial session terminated >>>");
        }


        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
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


        /// <summary>
        /// Displays help information about this application and its command-line arguments
        /// </summary>
        void ShowHelp()
        {
            Console.WriteLine("Usage: ss.exe [-com:PORT] [-baud:RATE] [-parity:PARITY] [-databits:VAL]");
            Console.WriteLine("              [-stopbits:VAL] [-autoconnect:VAL] [-log:LOGFILE] [-logmode:MODE]");
            Console.WriteLine("              [-quiet]\n");
            Console.WriteLine("A basic serial terminal for IoT device programming in general, and working with");
            Console.WriteLine("CircuitPython devices specifically.  With no command-line arguments specified,");
            Console.WriteLine("SimplySerial will attempt to identify and connect to a CircuitPython-capable board");
            Console.WriteLine("at 115200 baud, no parity, 8 data bits and 1 stop bit.  If no known boards are");
            Console.WriteLine("detected, it will default to the first available serial (COM) port at 9600 baud.\n");
            Console.WriteLine("Optional arguments:");
            Console.WriteLine("  -help             Display this help message");
            Console.WriteLine("  -version          Display version and installation information");
            Console.WriteLine("  -list             Display a list of available serial (COM) ports");
            Console.WriteLine("  -com:PORT         COM port number (i.e. 1 for COM1, 22 for COM22, etc.)");
            Console.WriteLine("  -baud:RATE        1200 | 2400 | 4800 | 7200 | 9600 | 14400 | 19200 | 38400 |");
            Console.WriteLine("                    57600 | 115200 | (Any valid baud rate for the specified port.)");
            Console.WriteLine("  -parity:PARITY    NONE | EVEN | ODD | MARK | SPACE");
            Console.WriteLine("  -databits:VAL     4 | 5 | 6 | 7 | 8");
            Console.WriteLine("  -stopbits:VAL     0 | 1 | 1.5 | 2");
            Console.WriteLine("  -autoconnect:VAL  NONE| ONE | ANY, enable/disable auto-(re)connection when");
            Console.WriteLine("                    a device is disconnected / reconnected.");
            Console.WriteLine("  -log:LOGFILE      Logs all output to the specified file.");
            Console.WriteLine("  -logmode:MODE     APPEND | OVERWRITE, default is OVERWRITE");
            Console.WriteLine("  -quiet            don't print any application messages/errors to console");
            Console.WriteLine("  -forcenewline     Force linefeeds (newline) in place of carriage returns in received data.");
            Console.WriteLine("  -encoding:ENC     UTF8 | ASCII | RAW");
            Console.WriteLine("  -noclear          Don't clear the terminal screen on connection.");
            Console.WriteLine("  -nostatus         Block status/title updates from virtual terminal sequences.");
            Console.WriteLine("\nPress CTRL-X to exit a running instance of SimplySerial.\n");

            // encoding:
            //if (argument[1].StartsWith("a"))
            //{
            //    _encoding = Encoding.ASCII;
            //    _convertToPrintable = false;
            //}
            //else if (argument[1].StartsWith("r"))
            //{
            //    _encoding = Encoding.GetEncoding(1252);
            //    _convertToPrintable = true;
            //}
            //else if (argument[1].StartsWith("u"))
            //{
            //    _encoding = Encoding.UTF8;
            //    _convertToPrintable = false;
            //}

        }


        /// <summary>
        /// Returns a list of available serial ports with their associated PID, VID and descriptions 
        /// Modified from the example written by Kamil Górski (freakone) available at
        /// http://blog.gorski.pm/serial-port-details-in-c-sharp
        /// https://github.com/freakone/serial-reader
        /// Some modifications were based on this stackoverflow thread:
        /// https://stackoverflow.com/questions/11458835/finding-information-about-all-serial-devices-connected-through-usb-in-c-sharp
        /// Hardware Bus Description through WMI is based on Simon Mourier's answer on this stackoverflow thread:
        /// https://stackoverflow.com/questions/69362886/get-devpkey-device-busreporteddevicedesc-from-win32-pnpentity-in-c-sharp
        /// </summary>
        /// <returns>List of available serial ports</returns>
        private List<ComPort> GetSerialPorts()
        {
            const string vidPattern = @"VID_([0-9A-F]{4})";
            const string pidPattern = @"PID_([0-9A-F]{4})";
            const string namePattern = @"(?<=\()COM[0-9]{1,3}(?=\)$)";
            const string query = "SELECT * FROM Win32_PnPEntity WHERE ClassGuid=\"{4d36e978-e325-11ce-bfc1-08002be10318}\"";

            // as per INTERFACE_PREFIXES in adafruit_board_toolkit
            // (see https://github.com/adafruit/Adafruit_Board_Toolkit/blob/main/adafruit_board_toolkit)
            string[] cpb_descriptions = new string[] { "CircuitPython CDC ", "Sol CDC ", "stringCarM0Ex CDC " };

            List<ComPort> detectedPorts = new List<ComPort>();

            foreach (var p in new ManagementObjectSearcher("root\\CIMV2", query).Get().OfType<ManagementObject>())
            {
                ComPort c = new ComPort();

                // extract and clean up port name and number
                c.name = p.GetPropertyValue("Name").ToString();
                Match mName = Regex.Match(c.name, namePattern);
                if (mName.Success)
                {
                    c.name = mName.Value;
                    c.num = int.Parse(c.name.Substring(3));
                }

                // if the port name or number cannot be determined, skip this port and move on
                if (c.num < 1)
                    continue;

                // get the device's VID and PID
                string pidvid = p.GetPropertyValue("PNPDeviceID").ToString();

                // extract and clean up device's VID
                Match mVID = Regex.Match(pidvid, vidPattern, RegexOptions.IgnoreCase);
                if (mVID.Success)
                    c.vid = mVID.Groups[1].Value.Substring(0, Math.Min(4, c.vid.Length));

                // extract and clean up device's PID
                Match mPID = Regex.Match(pidvid, pidPattern, RegexOptions.IgnoreCase);
                if (mPID.Success)
                    c.pid = mPID.Groups[1].Value.Substring(0, Math.Min(4, c.pid.Length));

                // extract the device's friendly description (caption)
                c.description = p.GetPropertyValue("Caption").ToString();

                // attempt to match this device with a known board
                //c.board = MatchBoard(c.vid, c.pid);

                // extract the device's hardware bus description
                c.busDescription = "";
                var inParams = new object[] { new string[] { "DEVPKEY_Device_BusReportedDeviceDesc" }, null };
                p.InvokeMethod("GetDeviceProperties", inParams);
                var outParams = (ManagementBaseObject[])inParams[1];
                if (outParams.Length > 0)
                {
                    var data = outParams[0].Properties.OfType<PropertyData>().FirstOrDefault(d => d.Name == "Data");
                    if (data != null)
                    {
                        c.busDescription = data.Value.ToString();
                    }
                }

                // we can determine if this is a CircuitPython board by its bus description
                //foreach (string prefix in cpb_descriptions)
                //{
                //    if (c.busDescription.StartsWith(prefix))
                //        c.isCircuitPython = true;
                //}

                // add this port to our list of detected ports
                detectedPorts.Add(c);
            }

            return detectedPorts;
        }
    }
}


