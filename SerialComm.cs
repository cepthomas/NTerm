using Ephemera.NBagOfTricks.Slog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace NTerm
{

    public class SerialComm : IComm
    {
        #region IComm implementation
        public int ResponseTime { get; set; } = 500;

        public int BufferSize { get; set; } = 4096;

        public string Response { get; private set; } = "";

        public OpStatus Send(string request) { return SendAsync(request).Result; }
        #endregion

        #region Fields
        readonly Logger _logger = LogManager.CreateLogger("SerialComm");
      //  string _host = "???";
        //int _portnum = 0;
      //  IPEndPoint _ipEndPoint;
        #endregion
        
        //List<ComPort> _availablePorts = new List<ComPort>();
        SerialPort? _serialPort = null;

        //// default comspec values and application settings set here will be overridden by values passed through command-line arguments
        //bool _quiet = false;
        //AutoConnect _autoConnect = AutoConnect.ONE;
        //ComPort _port = new ComPort();

        //int _baud = -1;
        //Parity _parity = Parity.None;
        //int _dataBits = 8;
        //StopBits _stopBits = StopBits.One;

        //bool logging = false;
        //FileMode logMode = FileMode.Create;
        //string logFile = string.Empty;
        //string logData = string.Empty;
        //int bufferSize = 102400;
        //DateTime lastFlush = DateTime.Now;
        //bool _forceNewline = false;
        //Encoding _encoding = Encoding.UTF8;
        //bool _convertToPrintable = false;
        //bool _clearScreen = true;
        //bool _noStatus = false;

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


        bool _continue;

        //public SerialComm()
        public SerialComm(string name, int baud, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One) //string[] args)
        {
            string message;
            StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;


            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }



            Thread readThread = new Thread(Read);

            // Create a new SerialPort object with default settings.
            _serialPort = new SerialPort(name, baud, parity, dataBits, stopBits);

            //// Allow the user to set the appropriate properties.
            //_serialPort.PortName = SetPortName(_serialPort.PortName);
            //_serialPort.BaudRate = SetPortBaudRate(_serialPort.BaudRate);
            //_serialPort.Parity = SetPortParity(_serialPort.Parity);
            //_serialPort.DataBits = SetPortDataBits(_serialPort.DataBits);
            //_serialPort.StopBits = SetPortStopBits(_serialPort.StopBits);
            //_serialPort.Handshake = SetPortHandshake(_serialPort.Handshake);

            // Set the read/write timeouts
            _serialPort.ReadTimeout = 500;
            _serialPort.WriteTimeout = 500;

            _serialPort.Open();
            _continue = true;
            readThread.Start();

            //Console.Write("Name: ");
            //name = Console.ReadLine();

            Console.WriteLine("Type QUIT to exit");

            while (_continue)
            {
                message = Console.ReadLine();

                if (stringComparer.Equals("quit", message))
                {
                    _continue = false;
                }
                else
                {
                    _serialPort.WriteLine(string.Format("<{0}>: {1}", name, message));
                }
            }

            readThread.Join();

            _serialPort.Close();
        }

        public void Read()
        {
            while (_continue)
            {
                try
                {
                    string message = _serialPort.ReadLine();
                    Console.WriteLine(message);
                }
                catch (TimeoutException) { }
            }
        }

        // Display Port values and prompt user to enter a port.
        public string SetPortName(string defaultPortName)
        {
            string portName;

            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter COM port value (Default: {0}): ", defaultPortName);
            portName = Console.ReadLine();

            if (portName == "" || !portName.ToLower().StartsWith("com"))
            {
                portName = defaultPortName;
            }
            return portName;
        }

        // Display BaudRate values and prompt user to enter a value.
        public int SetPortBaudRate(int defaultPortBaudRate)
        {
            string baudRate;

            Console.Write("Baud Rate(default:{0}): ", defaultPortBaudRate);
            baudRate = Console.ReadLine();

            if (baudRate == "")
            {
                baudRate = defaultPortBaudRate.ToString();
            }

            return int.Parse(baudRate);
        }

        // Display PortParity values and prompt user to enter a value.
        public Parity SetPortParity(Parity defaultPortParity)
        {
            string parity;

            Console.WriteLine("Available Parity options:");
            foreach (string s in Enum.GetNames(typeof(Parity)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Parity value (Default: {0}):", defaultPortParity.ToString(), true);
            parity = Console.ReadLine();

            if (parity == "")
            {
                parity = defaultPortParity.ToString();
            }

            return (Parity)Enum.Parse(typeof(Parity), parity, true);
        }

        // Display DataBits values and prompt user to enter a value.
        public int SetPortDataBits(int defaultPortDataBits)
        {
            string dataBits;

            Console.Write("Enter DataBits value (Default: {0}): ", defaultPortDataBits);
            dataBits = Console.ReadLine();

            if (dataBits == "")
            {
                dataBits = defaultPortDataBits.ToString();
            }

            return int.Parse(dataBits.ToUpperInvariant());
        }

        // Display StopBits values and prompt user to enter a value.
        public StopBits SetPortStopBits(StopBits defaultPortStopBits)
        {
            string stopBits;

            Console.WriteLine("Available StopBits options:");
            foreach (string s in Enum.GetNames(typeof(StopBits)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter StopBits value (None is not supported and \n" +
             "raises an ArgumentOutOfRangeException. \n (Default: {0}):", defaultPortStopBits.ToString());
            stopBits = Console.ReadLine();

            if (stopBits == "")
            {
                stopBits = defaultPortStopBits.ToString();
            }

            return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
        }

        public Handshake SetPortHandshake(Handshake defaultPortHandshake)
        {
            string handshake;

            Console.WriteLine("Available Handshake options:");
            foreach (string s in Enum.GetNames(typeof(Handshake)))
            {
                Console.WriteLine("   {0}", s);
            }

            Console.Write("Enter Handshake value (Default: {0}):", defaultPortHandshake.ToString());
            handshake = Console.ReadLine();

            if (handshake == "")
            {
                handshake = defaultPortHandshake.ToString();
            }

            return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
        }



        public async Task<OpStatus> SendAsync(string request)  // public void Run()
        {
            //_serialPort.BaseStream.ReadAsync();

            OpStatus res = OpStatus.Success;
            Response = "";

            // this is the core functionality - loop while the serial port is open
            while (_serialPort.IsOpen)
            {
                try
                {
                    using var stream = _serialPort.BaseStream;

                    // process keypresses for transmission through the serial port
                    if (Console.KeyAvailable)
                    {
                        // determine what key is pressed (including modifiers)
                        var keyInfo = Console.ReadKey(intercept: true);

                        // int num = bytes.Length;
                        int ind = 0;
                        int tosend = 1;


                        // exit the program if CTRL-X was pressed
                        if ((keyInfo.Key == ConsoleKey.X) && (keyInfo.Modifiers == ConsoleModifiers.Control))
                        {
//                            Output("\n<<< SimplySerial session terminated via CTRL-X >>>");
                            throw new();
                        }
                        // check for keys that require special processing (cursor keys, etc.)
                        else if (_specialKeys.ContainsKey(keyInfo.Key))
                        {
                            //_serialPort.Write(_specialKeys[keyInfo.Key]);
                            await stream.WriteAsync([(byte)(keyInfo.Key)], ind, tosend);

                        }

                        // everything else just gets sent right on through
                        else
                        {
                            await stream.WriteAsync([(byte)keyInfo.KeyChar], ind, tosend);
                            //_serialPort.Write(Convert.ToString(keyInfo.KeyChar));
                        }
                    }

                    /////// Receive ////////
                    List<string> parts = [];
                    bool rcvDone = false;

                    while (!rcvDone)
                    {
                        // Get response.
                        var buffer = new byte[BufferSize];

                        // If the read time-out expires, ReadAsync() throws IOException.
                        var byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);

                        if (byteCount > 0)
                        {
                            var s = Encoding.UTF8.GetString(buffer, 0, byteCount);
                            parts.Add(s);
                        }
                        else
                        {
                            rcvDone = true;
                        }
                    }

                    Response = string.Join("", parts);

                    _logger.Debug($"[Client] Server response was [{Response}]");





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
                    {
                        throw new IOException();
                    }
                }

                catch (IOException e)
                {
                    // Usually receive timeout.
                    // Ignore and retry later.
                    _logger.Debug($"{e.Message}: {e}");
                    Response = "Usually receive timeout.";
                    res = OpStatus.Timeout;
                }
                catch (Exception e)
                {
                    // Other errors are considered fatal.
                    _logger.Error($"Fatal error:{e}");
                    Response = $"Fatal error: {e.Message}";
                    res = OpStatus.Error;
                }
            }
            return res;
        } //while (_autoConnect > AutoConnect.NONE);


        public void Dispose()
        {
            _serialPort?.Close();
            _serialPort?.Dispose();
            _serialPort = null;
        }
    }
}
