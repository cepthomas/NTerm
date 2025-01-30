using System;
using System.Net.Sockets;
using System.Numerics;
using System.Text;


namespace NTermTest
{
    internal class Program
    {
        static void Main_TODO(string[] args)
        {
            bool ok = false;

            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "ansi":
                        ok = true;
                        Ansi.Run();
                        break;

                    default:
                        if (int.TryParse(args[1], out int result))
                        {
                            ok = true;
                            Server.Run(result);
                        }
                        break;
                }
            }

            if (!ok)
            {
                Console.WriteLine("Invalid args");
                Environment.Exit(1);
            }
        }
    }
}
