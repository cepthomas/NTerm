using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
#if TEST_UI
using System.Windows.Forms;
#endif


namespace Test
{
#if TEST_UI
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
#else // CL version
    internal class Program
    {
        static void Main()//string[] args)
        {
            int port = 59120;
            byte delim = 0; // LF=10  CR=13  NUL=0

            using CancellationTokenSource ts = new();

            try
            {
                TcpServer srv = new(port, delim, ts);
                //UdpServer srv = new(port, delim, ts);

                var err = srv.Run();
            }
            catch (Exception e)
            {
                
            }
        }
    }
#endif
}
