using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            //int port = 59120;
            byte delim = 0; // LF=10  CR=13  NUL=0

            using CancellationTokenSource ts = new();

            try
            {
                //TcpServer srv = new(59120, delim, ts);
                UdpSender srv = new(59140, delim, ts);

                var err = srv.Run();
            }
            catch (Exception e)
            {
                
            }
        }

        /////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////

        #region Fields
        // Generated test-specific config file.
        const string CONFIG_FILE = @"C:\Dev\Apps\NTerm\Test\test_config.ini";
        const string NTERM_EXE = @"C:\Dev\Apps\NTerm\bin\net8.0-windows\win-x64\NTerm.exe";
        // readonly ConsoleColorEx clr = ConsoleColorEx.None;
        #endregion

        /// <summary>
        /// Simple first test from cmd line. TODO also tcp/udp?
        /// </summary>
        void DoBasic()
        {
            Go("null");
        }

        /// <summary>
        /// Test config functions.
        /// </summary>
        void DoConfig()
        {
            // Tweak config.
            var config = BuildConfig("null");
            File.WriteAllLines(CONFIG_FILE, config);

            Go(CONFIG_FILE);
        }

        /// <summary>
        /// Test tcp in command/response mode.
        /// </summary>
        void DoTcpCmdResp()
        {
            // Tweak config.
            var config = BuildConfig("tcp 127.0.0.1 59120");
            File.WriteAllLines(CONFIG_FILE, config);

            Go(CONFIG_FILE);
        }

        /// <summary>
        /// Test udp in continuous mode.
        /// </summary>
        void DoUdpContinuous()
        {
            // Tweak config.
            var config = BuildConfig("udp 127.0.0.1 59140");
            File.WriteAllLines(CONFIG_FILE, config);

            Go(CONFIG_FILE);
        }

        /// <summary>
        /// Run the exe with full user cli.
        /// </summary>
        /// <param name="args"></param>
        void Go(string args)
        {
            ProcessStartInfo pinfo = new(NTERM_EXE, args)
            {
            };

            using Process proc = new() { StartInfo = pinfo };

            Console.WriteLine("Start process...");
            proc.Start();

            Console.WriteLine("Wait for exit...");
            proc.WaitForExit();

            Console.WriteLine("Exited...");
        }

        /// <summary>
        /// Run the exe but take ownership of the user cli.
        /// </summary>
        /// <param name="args"></param>
        void GoSteal(string args)
        {
            ProcessStartInfo pinfo = new(NTERM_EXE, args)
            {
                UseShellExecute = false,
                //CreateNoWindow = true,
                //WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // Add app-specific environmental variables.
            // pinfo.EnvironmentVariables["MY_VAR"] = "Hello!";

            using Process proc = new() { StartInfo = pinfo };
            //proc.Exited += (sender, e) => { LogInfo("Process exit event."); };

            Console.WriteLine("Start process...");
            proc.Start();

            // TIL: To avoid deadlocks, always read the output stream first and then wait.
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();

            //LogInfo("Wait for process to exit...");
            proc.WaitForExit();

            Console.WriteLine("Exited...");
            // return new(proc.ExitCode, stdout, stderr);
        }

        /// <summary>
        /// Clone and mod the default config.
        /// </summary>
        /// <param name="commType"></param>
        /// <returns></returns>
        List<string> BuildConfig(string commType)
        {
            List<string> defaultConfig = [
                "[nterm]",
                //"comm_type = null",
                "delim = NUL",
                "prompt = >",
                "meta = -",
                "info_color = darkcyan",
                "err_color = green",
                "[macros]",
                "dox = \"do xxxxxxx\"",
                "s3 = \"hey, send 333333333\"",
                "tm = \"  xmagentax   -yellow-  \"",
                "[matchers]",
                "\"mag\" = magenta",
                "\"yel\" = yellow"];

            // Clone and mod defaultConfig => comm_type = null
            List<string> config = [];
            defaultConfig.ForEach(l =>
            {
                config.Add(new(l));
                if (l.Contains("[nterm]"))
                {
                    config.Add($"comm_type = {commType}");
                }
            });

            return config;
        }
    }


}
