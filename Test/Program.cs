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


namespace Test
{
    internal class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] _)
        {
            // Make sure out path exists.
             var outPath = Path.Join(MiscUtils.GetSourcePath(), "out");
            DirectoryInfo di = new(outPath);
            di.Create();

            // Run pnut tests from cmd line.
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "TERM" };
            runner.RunSuites(cases);

            //var fn = Path.Combine(MiscUtils.GetSourcePath(), "out", "pnut_out.txt");
            //File.WriteAllLines(fn, runner.Context.OutputLines);

            //runner.Context.OutputLines.ForEach(l => Console.WriteLine(l));

            Environment.Exit(0);
        }
    }
}
