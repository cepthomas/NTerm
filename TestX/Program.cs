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
        static void Main(string[] args)
        {
            int port = 59120;
            byte delim = 0; // LF=10  CR=13  NUL=0

            using CancellationTokenSource ts = new();

            try
            {
                TcpServer srv = new(port, delim, ts);
                var err = srv.Run(); 
            }
            catch (Exception e)
            {
                
            }
        }

        /// <summary>
        /// Write a line to console.
        /// </summary>
        /// <param name="cat">Category</param>
        /// <param name="text">What to print</param>
        public static void Print(Cat cat, string text)
        {
            var catColor = cat switch
            {
                Cat.Error => _errorColor,
                Cat.Info => _infoColor,
                _ => ConsoleColorEx.None
            };

            //  If color not explicitly specified, look for text matches.
            if (catColor == ConsoleColorEx.None)
            {
                foreach (var m in _matchers)
                {
                    if (text.Contains(m.Key)) // faster than compiled regexes
                    {
                        catColor = m.Value;
                        break;
                    }
                }
            }

            if (catColor != ConsoleColorEx.None)
            {
                Console.ForegroundColor = (ConsoleColor)catColor;
            }

            Console.Write(text);
            Console.Write(Environment.NewLine);
            Console.ResetColor();
            
            Log(cat, text);
        }


    }
}
