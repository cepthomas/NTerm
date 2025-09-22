using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;


namespace TestX
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
