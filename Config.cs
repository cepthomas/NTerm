using Ephemera.NBagOfTricks;
using System;
using System.Collections.Generic;
using System.IO;

namespace NTerm
{
    /// <summary>Reporting user config errors.</summary>
    public class ConfigException(string message) : Exception(message);

    public class Config
    {
        #region Config properties
        /// <summary>Comm parameters.</summary>
        public List<string> CommType { get; private set; } = [];

        /// <summary>Color for error messages.</summary>
        public ConsoleColorEx ErrorColor { get; private set; } = ConsoleColorEx.Red; // default

        /// <summary>Color for internal messages.</summary>
        public ConsoleColorEx InfoColor { get; private set; } = ConsoleColorEx.Blue; // default

        /// <summary>Prompt. Can be empty for continuous receiving.</summary>
        public string Prompt { get; private set; } = ""; // default

        /// <summary>Message delimiter: LF=10 CR=13 NUL=0.</summary>
        public byte Delim { get; private set; } = 0; // default NUL

        /// <summary>User macros.</summary>
        public Dictionary<string, string> Macros { get; private set; } = [];

        /// <summary>Colorizing text.</summary>
        public Dictionary<string, ConsoleColorEx> Matchers { get; private set; } = [];
        #endregion

        /// <summary>
        /// Decipher the user args.
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="ConfigException"></exception>
        public void Load(List<string> args)
        {
            // Check for ini file first.
            if (args[0].EndsWith(".ini"))
            {
                if (!File.Exists(args[0]))
                {
                    throw new ConfigException($"Invalid config file: [{args[0]}]");
                }

                // OK process it.
                var inrdr = new IniReader();
                inrdr.ParseFile(args[0]);
                var ntermSect = inrdr.GetValues("nterm");

                // [nterm] section
                foreach (var kv in ntermSect)
                {
                    switch (kv.Key.ToLower())
                    {
                        case "comm_type":
                            CommType = kv.Value.SplitByToken(" ");
                            // Process comm spec.
                            List<string> valid = ["null", "tcp", "udp", "serial"];
                            if (CommType.Count < 1 || !valid.Contains(CommType[0]))
                            {
                                throw new ConfigException($"Invalid comm type: [{kv}]");
                            }
                            break;

                        case "err_color":
                            ErrorColor = Enum.Parse<ConsoleColorEx>(kv.Value, true);
                            break;

                        case "info_color":
                            InfoColor = Enum.Parse<ConsoleColorEx>(kv.Value, true);
                            break;

                        case "prompt":
                            Prompt = kv.Value;
                            break;

                        case "delim":
                            Delim = kv.Value switch
                            {
                                "LF" => 10,
                                "CR" => 13,
                                "NUL" => 0,
                                _ => throw new ConfigException($"Invalid delim: [{kv.Value}]"),
                            };
                            break;

                        default:
                            throw new ConfigException($"Invalid [nterm] section key: [{kv.Key}]");
                    }
                }

                // [macros] section
                ntermSect = inrdr.GetValues("macros");
                ntermSect.ForEach(kv => Macros[kv.Key] = kv.Value.Replace("\"", ""));

                // [matchers] section
                ntermSect = inrdr.GetValues("matchers");
                ntermSect.ForEach(val => Matchers[val.Key.Replace("\"", "")] = Enum.Parse<ConsoleColorEx>(val.Value, true));
            }
            else // assume explicit cl spec
            {
                CommType = args;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<string> Doc()
        {
            List<string> ls = [];

            ls.Add($"comm_type:{string.Join(" ", CommType)}");
            ls.Add($"delim:{Delim}");
            ls.Add($"prompt:{Prompt}");
            ls.Add($"info_color:{InfoColor}");
            ls.Add($"err_color:{ErrorColor}");

            if (Macros.Count > 0)
            {
                ls.Add($"macros:");
                Macros.ForEach(m => ls.Add($"    {m.Key}:{m.Value}"));
            }

            if (Matchers.Count > 0)
            {
                ls.Add($"matchers:");
                Matchers.ForEach(m => ls.Add($"    {m.Key}:{m.Value}"));
            }

            return ls;
        }
    }
}
