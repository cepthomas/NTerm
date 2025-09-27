using Ephemera.NBagOfTricks;
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


namespace Test
{

    internal class Program
    {
        static void Main() { new Program().Run(); }

        #region Fields
        /// <summary>User input</summary>
        readonly ConcurrentQueue<string> _qUserCli = new();

        /// <summary>LF=10  CR=13  NUL=0</summary>
        byte _delim = 0;

        /// <summary>Config to use</summary>
        string _configFile = "???";

        /// <summary>Target executable</summary>
        string _ntermExe = "???";
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public void Run()
        {
            Console.WriteLine($"========= Test =========");
            _configFile = Path.Combine(MiscUtils.GetSourcePath(), "test_config.ini");
            _ntermExe = Path.Combine(MiscUtils.GetSourcePath(), "..", "bin", "net8.0-windows", "win-x64", "NTerm.exe");

            using CancellationTokenSource ts = new();
            //using Task taskKeyboard = Task.Run(() => _qUserCli.Enqueue(Console.ReadLine() ?? ""));

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
        }

        /// <summary>
        /// Simple first test from cmd line. TODO also tcp/udp?
        /// </summary>
        void DoBasicTarget(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoBasicTarget()");
            List<string> config = [
                "[nterm]", "comm_type = null", "delim = NUL", "prompt = >", "meta = -"];
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
                "[nterm]", "comm_type = null", "delim = NUL", "prompt = >", "meta = -",
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
                "[nterm]", "comm_type = tcp 127.0.0.1 59120", "delim = NUL", "prompt = >", "meta = -",
                "info_color = darkcyan", "err_color = green",
            "[macros]", "dox = \"do xxxxxxx\"", "s3 = \"hey, send 333333333\"", "tm = \"  xmagentax   -yellow-  \"",
            "[matchers]", "\"mag\" = magenta", "\"yel\" = yellow"];
            File.WriteAllLines(_configFile, config);
            var proc = RunTarget(_configFile);
            TcpServer srv = new(59120, _delim);
            var err = srv.Run(ts);
        }

        /// <summary>
        /// Test udp in continuous mode.
        /// </summary>
        void DoUdpTarget(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoUdpTarget()");
            // Tweak config.
            List<string> config = [
                "[nterm]", "comm_type = udp 127.0.0.1 59140", "delim = NUL", "prompt = >", "meta = -",
                "info_color = darkcyan", "err_color = green",
            "[macros]", "dox = \"do xxxxxxx\"", "s3 = \"hey, send 333333333\"", "tm = \"  xmagentax   -yellow-  \"",
            "[matchers]", "\"mag\" = magenta", "\"yel\" = yellow"];
            File.WriteAllLines(_configFile, config);
            var proc = RunTarget(_configFile);
            UdpSender srv = new(59140, _delim);
            srv.Run(ts);
        }

        /// <summary>
        /// Test tcp in command/response mode.
        /// </summary>
        void DoTcpDebugger(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoTcpDebugger()");
            // Runs forever.
            TcpServer srv = new(59120, _delim);
            srv.Run(ts);
        }

        /// <summary>
        /// Test udp in continuous mode.
        /// </summary>
        void DoUdpDebugger(CancellationTokenSource ts)
        {
            Console.WriteLine($"DoUdpDebugger()");
            // Always do once.
            UdpSender srv = new(59140, _delim);
            srv.Run(ts);
        }

        /// <summary>
        /// Run the exe with full user cli.
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
