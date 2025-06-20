using System;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Ephemera.NBagOfTricks.PNUT;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using NTerm;


namespace Test
{
    static class Program
    {
        static void Main(string[] args)
        {
            //UserSettings ss = (UserSettings)SettingsCore.Load(MiscUtils.GetSourcePath(), typeof(UserSettings), "fake_settings.json");

            Console.WriteLine("Hello, Test!");

            //FakeSettings();

            Server.Run(8888);// int.Parse(ss.Configs[0].Args[1]));
        }


        static void FakeSettings()
        {
            //UserSettings ss = (UserSettings)SettingsCore.Load(MiscUtils.GetSourcePath(), typeof(UserSettings), "fake_settings.json");
            //ss.Configs.Clear();

            //ss.Prompt = ">>>";

            //for (int i = 0; i < 5; i++)
            //{
            //    var c = new Config()
            //    {
            //        Name = $"config{i}",
            //        Args = [$"a{i}", $"b{i}", $"c{i}"]
            //    };

            //    c.HotKeys.Add($"x=bla bla");
            //    c.HotKeys.Add($"y=foo bar");
            //    c.HotKeys.Add($"z=bleep bloop blop");

            //    ss.Configs.Add(c);
            //}

            //ss.CurrentConfig = "";

            //var eds = SettingsEditor.Edit(ss, "Fake", 500);
            //ss.Save();
        }
    }
}
