using System;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Ephemera.NBagOfTricks.PNUT;
using NTerm;
using Ephemera.NBagOfTricks;


namespace NTermTest // TODO1 fix all tests
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] _)
        {
            // Run pnut tests from cmd line.
            TestRunner runner = new(OutputFormat.Readable);
            //var cases = new[] { "ANSI" };
            var cases = new[] { "ANSI", "SER", "TCP" };
            runner.RunSuites(cases);
            File.WriteAllLines(Path.Join(MiscUtils.GetSourcePath(), "test_out.txt"), runner.Context.OutputLines);
        }

        static void FakeSettings()
        {
            UserSettings ss = (UserSettings)SettingsCore.Load(".", typeof(UserSettings), "fake_settings.json");
            ss.Configs.Clear();

            //var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            //_settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            //public static object Load(string dir, Type t, string fn = "settings.json")

            //Random rnd = new();

            //ss.Prompt = "!!!";

            for (int i = 0; i < 5; i++)
            {
                var c = new Config()
                {
                    Name = $"config{i}",
                    Args = [$"a{i}", $"b{i}", $"c{i}"]
                };

                c.HotKeys.Add($"x=bla bla");
                c.HotKeys.Add($"y=foo bar");
                c.HotKeys.Add($"z=bleep bloop blop");

                ss.Configs.Add(c);
            }

            ss.CurrentConfig = "";
            //var ed = new Editor() { Settings = ss };
            //ed.ShowDialog();
            ss.Save();
            //LoadSettings();
        }
    }
}
