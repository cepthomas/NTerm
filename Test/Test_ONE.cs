using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using NTerm;


 // TODO1 Rethink tests - what is useful? Use with IConsole etc.

namespace Test
{
    public class TERM_MAIN : TestSuite
    {
        #region Fields
        /// <summary>Config to use</summary>
        string _configFile = "???";

        /// <summary>Target executable</summary>
        string _ntermExe = "???";
        #endregion

        public override void RunSuite()
        {
            Console.Read();
            Console.WriteLine($"========= Test =========");
            _configFile = Path.Combine(MiscUtils.GetSourcePath(), "test_config.ini");
            _ntermExe = Path.Combine(MiscUtils.GetSourcePath(), "..", "bin", "net8.0-windows", "win-x64", "NTerm.exe");
            using CancellationTokenSource ts = new();
            //using Task taskKeyboard = Task.Run(() => _qUserCli.Enqueue(Console.ReadLine() ?? ""));


//            App app = new();


            try
            {
                // Target flavors run binary NTerm.exe.
                //DoBasicTarget(ts);
                //DoConfigTarget(ts);
                //DoTcpTarget(ts);
                DoUdpTarget(ts);

                // Debugger flavors require starting NTerm with matching cmd line.
                //DoTcpDebugger(ts);
                //DoUdpDebugger(ts);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fatal!! {e}");
                //Task.WaitAll([taskKeyboard]);
            }

            // UT_INFO("Test ini reader.");

            // var inputDir = Path.Join(MiscUtils.GetSourcePath(), "Files");
            // // var outputDir = Path.Join(MiscUtils.GetSourcePath(), "out");

            // var irdr = new IniReader();
            // irdr.ParseFile(Path.Join(inputDir, "valid.ini"));
            // var sections = irdr.GetSectionNames();
            // UT_EQUAL(sections.Count, 5);

            // UT_EQUAL(irdr.GetValues("test123").Count, 5);
            // UT_EQUAL(irdr.GetValues("Some lists").Count, 2);

            // UT_THROWS(typeof(InvalidOperationException), () =>
            // {
            //     irdr.GetValues("not here lists");
            // });
        }

        /// <summary>
        /// Simple first test from cmd line. TODO also tcp/udp?
        /// </summary>
        void DoBasicTarget(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoBasicTarget()");
            List<string> config = ["[nterm]", "comm = null", "delim = NUL", "xxxx", "zzz"];
            File.WriteAllLines(_configFile, config);
            var proc = RunTarget(_configFile);
        }

        /// <summary>
        /// Test config functions.
        /// </summary>
        void DoConfigTarget(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoConfigTarget()");
            List<string> config = [
                "[nterm]", "comm = null", "delim = NUL", "xxxx", "zzz",
                "info_color = darkcyan", "err_color = green",
            "[macros]", "dox = \"do xxxxxxx\"", "s3 = \"hey, send 333333333\"", "tm = \"  xmagentax   -yellow-  \"",
            "[matchers]", "\"mag\" = magenta", "\"yel\" = yellow"];
            File.WriteAllLines(_configFile, config);
            var proc = RunTarget(_configFile);
        }

        /// <summary>
        /// Test tcp in command/response mode.
        /// </summary>
        void DoTcpTarget(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoTcpTarget()");
            // Tweak config.
            List<string> config = [
                "[nterm]", "comm = tcp 127.0.0.1 59120", "delim = NUL", "xxxx", "zzz",
                "info_color = darkcyan", "err_color = green",
            "[macros]", "dox = \"do xxxxxxx\"", "s3 = \"hey, send 333333333\"", "tm = \"  xmagentax   -yellow-  \"",
            "[matchers]", "\"mag\" = magenta", "\"yel\" = yellow"];
            File.WriteAllLines(_configFile, config);
            var proc = RunTarget(_configFile);
//TcpServer srv = new(59120, _delim);
//var err = srv.Run(ts);
        }

        /// <summary>
        /// Test udp in continuous mode.
        /// </summary>
        void DoUdpTarget(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoUdpTarget()");
            // Tweak config.
            List<string> config = [
                "[nterm]", "comm = udp 127.0.0.1 59140", "delim = NUL", "xxxx", "zzz",
                "info_color = darkcyan", "err_color = green",
            "[macros]", "dox = \"do xxxxxxx\"", "s3 = \"hey, send 333333333\"", "tm = \"  xmagentax   -yellow-  \"",
            "[matchers]", "\"mag\" = magenta", "\"yel\" = yellow"];
            File.WriteAllLines(_configFile, config);
            var proc = RunTarget(_configFile);
//UdpSender srv = new(59140, _delim);
//srv.Run(ts);
        }

        /// <summary>
        /// Test tcp in command/response mode.
        /// </summary>
        void DoTcpDebugger(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoTcpDebugger()");
            // Runs forever.
//TcpServer srv = new(59120, _delim);
//srv.Run(ts);
        }

        /// <summary>
        /// Test udp in continuous mode.
        /// </summary>
        void DoUdpDebugger(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoUdpDebugger()");
            // Always do once.
//UdpSender srv = new(59140, _delim);
//srv.Run(ts);
        }

        /// <summary>
        /// Run the exe with full user cli. => use new util
        /// </summary>
        /// <param name="args"></param>
        Process RunTarget(string args, bool capture = false)
        {
            ProcessStartInfo pinfo = new(_ntermExe, args)
            {
                UseShellExecute = !capture,
                RedirectStandardOutput = capture,
                RedirectStandardError = capture,
            };

            using Process proc = new() { StartInfo = pinfo };

            Console.WriteLine("Start process...");
            proc.Start();

            // if (capture)
            // {
            //     // TIL: To avoid deadlocks, always read the output stream first and then wait.
            //     var stdout = proc.StandardOutput.ReadToEnd();
            //     var stderr = proc.StandardError.ReadToEnd();
            // }

            //Console.WriteLine("Wait for exit...");
            //proc.WaitForExit();
            //Console.WriteLine("Exited...");

            // if (capture)
            // {
            //     return new(proc.ExitCode, stdout, stderr);
            // }

            return proc;
        }
    }
}
