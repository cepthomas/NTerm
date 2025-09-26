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

        /// <summary>Indicator for application functions.</summary>
        public char MetaInd { get; private set; } = '!'; // default

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
                var inrdr = new IniReader(args[0]);

                if (!inrdr.Contents.TryGetValue("nterm", out IniSection? ntermSect))
                {
                    throw new ConfigException($"Section [nterm] is required");
                }

                // [nterm] section
                foreach (var nval in ntermSect.Values)
                {
                    switch (nval.Key.ToLower())
                    {
                        case "comm_type":
                            CommType = nval.Value.SplitByToken(" ");
                            // Process comm spec.
                            List<string> valid = ["null", "tcp", "udp", "serial"];
                            if (CommType.Count < 1 || !valid.Contains(CommType[0]))
                            {
                                throw new ConfigException($"Invalid comm type: [{nval}]");
                            }
                            break;

                        case "err_color":
                            ErrorColor = Enum.Parse<ConsoleColorEx>(nval.Value, true);
                            break;

                        case "info_color":
                            InfoColor = Enum.Parse<ConsoleColorEx>(nval.Value, true);
                            break;

                        case "prompt":
                            Prompt = nval.Value;
                            break;

                        case "meta":
                            MetaInd = nval.Value[0];
                            break;

                        case "delim":
                            Delim = nval.Value switch
                            {
                                "LF" => 10,
                                "CR" => 13,
                                "NUL" => 0,
                                _ => throw new ConfigException($"Invalid delim: [{nval.Value}]"),
                            };
                            break;

                        default:
                            throw new ConfigException($"Invalid [nterm] section key: [{nval.Key}]");
                    }
                }

                // [macros] section
                if (inrdr.Contents.TryGetValue("macros", out IniSection? macroSect))
                {
                    macroSect.Values.ForEach(val => Macros[val.Key] = val.Value.Replace("\"", ""));
                }

                // [matchers] section
                if (inrdr.Contents.TryGetValue("matchers", out IniSection? matcherSect))
                {
                    matcherSect.Values.ForEach(val => Matchers[val.Key.Replace("\"", "")] = Enum.Parse<ConsoleColorEx>(val.Value, true));
                }
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

            ls.Add($"Current Configuration");
            ls.Add($"- comm_type:{string.Join(" ", CommType)}");
            ls.Add($"- delim:{Delim}");
            ls.Add($"- prompt:{Prompt}");
            ls.Add($"- meta:{MetaInd}");
            ls.Add($"- info_color:{InfoColor}");
            ls.Add($"- err_color:{ErrorColor}");

            if (Macros.Count > 0)
            {
                ls.Add($"Macros:");
                Macros.ForEach(m => ls.Add($"- {m.Key}:{m.Value}"));
            }

            if (Matchers.Count > 0)
            {
                ls.Add($"Matchers:");
                Matchers.ForEach(m => ls.Add($"- {m.Key}:{m.Value}"));
            }

            return ls;
        }
    }
}
