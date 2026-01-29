using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;



/* TODO1 these:

App TestMode
- Run app with NullComm. Everything should work ok from UI POV.
- Run app with TcpComm. Run Test\tcp_server.py - capture process stdout and Print().Debug.
- Same for UdpComm.

PNUT Test
- Use MockConsole. Add IConsole to App().
- Run tests like in TEST_One.

C:\Dev\Apps\NTerm\Properties\launchSettings.json:
    5:       "commandLineArgs": "C:\\Dev\\Apps\\NTerm\\Test\\test_config.ini"

C:\Dev\Apps\NTerm\Test\Test_ONE.cs:
   46:             _configFile = Path.Combine(MiscUtils.GetSourcePath(), "test_config.ini");
   80:             // irdr.ParseFile(Path.Join(inputDir, "valid.ini"));

*/

namespace NTerm
{
    internal class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var console = new RealConsole();

            using var app = new App(console);
        }
    }
}
