using System;
using System.Net.Sockets;
using System.Numerics;
using System.Text;


// Works:
//AsyncUsingTcpClient();
//StartServer(_config.Port);
//var res = _comm.Send("djkdjsdfksdf;s");
//Write($"{res}: {_comm.Response}");
//var ser = new Serial();
//var ports = ser.GetSerialPorts();


namespace NTermTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            bool ok = false;

            if (args.Length == 1)
            {
                switch (args[0])
                {
                    //case "ansi":
                    //    ok = true;
                    //    Ansi.Run();
                    //    break;

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

        static void FakeSettings()
        {
            UserSettings ss = (UserSettings)SettingsCore.Load(".", typeof(UserSettings), "fake_settings.json");
            ss.Configs.Clear();

            //var appDir = MiscUtils.GetAppDataDir("NTerm", "Ephemera");
            //_settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            //public static object Load(string dir, Type t, string fn = "settings.json")

            //Random rnd = new();

            ss.Prompt = "!!!";

            for (int i = 0; i < 5; i++)
            {
                var c = new Config()
                {
                    Name = $"config{i}",
                    Args = $"a{i} b{i} c{i}",
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
