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
        #region Config properties
        /// <summary>Comm parameters.</summary>
        public List<string> CommConfig { get; private set; } = [];

        /// <summary>Color for error messages.</summary>
        public ConsoleColor ErrorColor { get; private set; } = ConsoleColor.Red;

        /// <summary>Color for comm messages.</summary>
        public ConsoleColor TrafficColor { get; private set; } = ConsoleColor.Yellow;

        /// <summary>Message delimiter: LF=10 CR=13 NUL=0.</summary>
        public byte Delim { get; private set; } = 0;

        /// <summary>User macros.</summary>
        public Dictionary<char, string> Macros { get; private set; } = [];

        /// <summary>Colorizing text.</summary>
        public Dictionary<string, ConsoleColor> Matchers { get; private set; } = [];

        /// <summary>Debug: Color for internal messages.</summary>
        public ConsoleColor DebugColor { get; private set; } = ConsoleColor.Cyan;

        /// <summary>Debug: Specific script to run.</summary>
        public string? DebugScript { get; private set; }
        #endregion

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
                            // Process comm spec.
                            List<string> valid = ["null", "tcp", "udp", "serial"];
                            if (CommConfig.Count < 1 || !valid.Contains(CommConfig[0]))
                            {
                                throw new ConfigException($"Invalid comm type: [{kv}]");
                            }
                            break;

                        case "error_color":
                            ErrorColor = Enum.Parse<ConsoleColor>(kv.Value, true);
                            break;

                        case "traffic_color":
                            TrafficColor = Enum.Parse<ConsoleColor>(kv.Value, true);
                            break;

                        case "debug_color":
                            DebugColor = Enum.Parse<ConsoleColor>(kv.Value, true);
                            break;

                        case "delim":
                            Delim = kv.Value switch
                            {
                                "LF" => ControlChar.LF,
                                "CR" => ControlChar.CR,
                                "NUL" => ControlChar.NUL,
                                _ => throw new ConfigException($"Invalid delim: [{kv.Value}]"),
                            };
                            break;

                        case "debug_script":
                            var dbgfn = MiscUtils.RationalizeFileName(kv.Value, Path.GetDirectoryName(iniFn));
                            if (dbgfn is null) { throw new ConfigException($"Invalid script: [{kv}]"); }
                            DebugScript = dbgfn;
                            break;

                        default:
                            throw new ConfigException($"Invalid [nterm] section key: [{kv.Key}]");
                    }
                }

                // [macros] section
                if (inrdr.GetSectionNames().Contains("macros"))
                {
                    ntermSect = inrdr.GetValues("macros");
                    ntermSect.ForEach(kv => Macros[kv.Key[0]] = kv.Value.Replace("\"", ""));
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
        public List<string> Doc()
        {
            List<string> ls = [];

            ls.Add($"comm:{string.Join(" ", CommConfig)}");
            ls.Add($"delim:{Delim}");
            ls.Add($"error_color:{ErrorColor}");
            ls.Add($"traffic_color:{TrafficColor}");

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
