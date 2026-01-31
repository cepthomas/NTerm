using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    internal class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            //- test_config_null.ini Run app with NullComm. Everything should work ok from UI POV.

            //- test_config_tcp.ini  Run app with TcpComm => .\Test\tcp_server.py - capture process stdout and Print().Debug.

            //- test_config_udp.ini  Run app with UdpComm => .\Test\udp_server.py - capture process stdout and Print().Debug.

            using var app = new App([.. args]);
        }
    }
}
