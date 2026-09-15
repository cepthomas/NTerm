using System;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    /// <summary>Reporting user config errors.</summary>
    public class ConfigException(string message) : Exception(message);

    public class Config
    {
        /// <summary>Properties per specific comm type.</summary>
        public List<string> CommConfig { get; private set; } = [];

        /// <summary>Color for error messages.</summary>
        public ConsoleColor? ErrorColor { get; private set; } = null;

        /// <summary>Debug: Color for internal messages.</summary>
        public ConsoleColor? DebugColor { get; private set; } = null;

        /// <summary>Indicates meta command next.</summary>
        public char MetaInd { get; private set; } = (char)ConsoleKey.Escape;

        /// <summary>User macros.</summary>
        public Dictionary<string, string> Macros { get; private set; } = [];

        /// <summary>Colorizing text.</summary>
        public Dictionary<string, ConsoleColor> Matchers { get; private set; } = [];


        /// <summary>
        /// Decipher the user args.
        /// </summary>
        /// <param name="args">From command line</param>
        /// <param name="defaultConfig">Default config file name</param>
        /// <exception cref="ConfigException"></exception>
        public void Load(List<string> args, string defaultConfig)
        {
            // Default first.
            ParseIni(defaultConfig);

            // Then a cmd line ini file maybe.
            if (args[0].EndsWith(".ini"))
            {
                // OK process it.
                ParseIni(args[0]);
            }
            else // assume explicit cl spec
            {
                CommConfig = args;
            }

            // Local fie processor.
            void ParseIni(string iniFn)
            {
                var inrdr = new IniReader();
                inrdr.ParseFile(iniFn);

                var ntermSect = inrdr.GetValues("nterm");

                // [nterm] section
                foreach (var kv in ntermSect)
                {
                    switch (kv.Key.ToLower())
                    {
                        case "comm":
                            CommConfig = kv.Value.SplitByToken(" ");
                            List<string> valid = ["null", "tcp", "udp", "serial"];
                            if (CommConfig.Count < 1 || !valid.Contains(CommConfig[0]))
                            {
                                throw new ConfigException($"Invalid comm type: [{kv}]");
                            }
                            break;

                        case "error_color":
                            ErrorColor = Enum.Parse<ConsoleColor>(kv.Value, true);
                            break;

                        case "debug_color":
                            DebugColor = Enum.Parse<ConsoleColor>(kv.Value, true);
                            break;

                        case "meta":
                            if (kv.Value.Equals("ESC", StringComparison.CurrentCultureIgnoreCase))
                            {
                                MetaInd = (char)ConsoleKey.Escape;
                            }
                            else if (kv.Value.Length == 1)
                            {
                                MetaInd = kv.Value[0];
                            }
                            else
                            {
                                throw new ConfigException($"Invalid meta ind: [{kv.Value}]");
                            }
                            break;

                        default:
                            throw new ConfigException($"Invalid [nterm] section key: [{kv.Key}]");
                    }
                }

                // [macros] section
                if (inrdr.GetSectionNames().Contains("macros"))
                {
                    ntermSect = inrdr.GetValues("macros");
                    ntermSect.ForEach(kv => Macros[kv.Key] = kv.Value.Replace("\"", ""));
                }

                // [matchers] section
                if (inrdr.GetSectionNames().Contains("matchers"))
                {
                    ntermSect = inrdr.GetValues("matchers");
                    ntermSect.ForEach(val => Matchers[val.Key.Replace("\"", "")] = Enum.Parse<ConsoleColor>(val.Value, true));
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            List<string> ls = [];

            ls.Add($"comm:{string.Join(" ", CommConfig)}");
            ls.Add($"meta_ind:{MetaInd}");
            ls.Add($"error_color:{ErrorColor}");

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

            return string.Join(Environment.NewLine, ls);
        }
    }
}
