using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace Test
{
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
                //UdbServer srv = new(port, delim, ts);

                var err = srv.Run();
            }
            catch (Exception e)
            {
                
            }
        }
    }
}



/* winforms version
using System.Windows.Forms;
namespace Test
{
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
}
*/
