using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
// using Ephemera.NBagOfTricks;
// using Ephemera.NBagOfTricks.Slog;


namespace NTerm
{
    public class Commander
    {
        #region Fields
        /// <summary>All the commands.</summary>
        readonly CommandDescriptor[] _commands;
        #endregion

        #region Types
        /// <summary>Command descriptor.</summary>
        readonly record struct CommandDescriptor
        (
            /// <summary>If you like to type.</summary>
            string LongName,
            /// <summary>If you don't.</summary>
            char ShortName,
            /// <summary>Free text for command description.</summary>
            string Info,
            /// <summary>Free text for args description.</summary>
            string Args,
            /// <summary>The runtime handler.</summary>
            CommandHandler Handler
        );

        /// <summary>Command handler.</summary>
        delegate bool CommandHandler(CommandDescriptor cmd, List<string> args);
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits stuff.
        /// </summary>
        /// <param name="scriptFn">Cli version requires cl script name.</param>
        /// <param name="tin">Stream in</param>
        /// <param name="tout">Stream out</param>
        public Commander()//string scriptFn, IConsole console)
        {
            State.Instance.ValueChangeEvent += State_ValueChangeEvent;
            
            _commands =
            [
                new("help",     '?',  "available commands",            "",                      UsageCmd),
                new("info",     'i',  "system information",            "",                      InfoCmd),
                new("exit",     'q',  "exit the application",          "",                      ExitCmd),
                new("run",      'r',  "toggle running the script",     "",                      RunCmd),
                new("position", 'p',  "set position or tell current",  "(pos)",                 PositionCmd),
                new("loop",     'l',  "set loop or tell current",      "(start end)",           LoopCmd),
                new("rewind",   'w',  "rewind loop",                   "",                      RewindCmd),
                new("tempo",    't',  "get or set the tempo",          "(40-240)",              TempoCmd),
                new("monitor",  'm',  "toggle monitor midi traffic",   "(r=rcv|s=snd|o=off)",   MonCmd),
                new("kill",     'k',  "stop all midi",                 "",                      KillCmd),
                new("reload",   's',  "reload current script",         "",                      ReloadCmd)
            ];

            try
            {
                // Script file validity checked in LoadScript().
                _logger.Info($"Loading script file {scriptFn}");
                _core.LoadScript(scriptFn);

                // Done. Wait a bit in case there are some lingering events or logging.
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                var (fatal, msg) = Utils.ProcessException(ex);
                if (fatal)
                {
                    _logger.Error(msg);
                }
                else
                {
                    // User can decide what to do with this. They may be recoverable so use warn.
                    State.Instance.ExecState = ExecState.Idle;
                    _logger.Warn(msg);
                }
            }
        }

        /// <summary>
        /// Loop forever doing cmdproc requests. Should not throw. Command processor will take care of its own errors.
        /// </summary>
        public void Run()
        {
            while (State.Instance.ExecState != ExecState.Exit)
            {
                DoCommand();
                // Don't be greedy.
                Thread.Sleep(20);
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Process user input. Blocks until new line or spacebar.
        /// </summary>
        /// <returns>Success</returns>
        public bool DoCommand(string ucmd)
        {
            bool ret = true;


            if (key.KeyChar == ' ')
            {
                // Toggle run.
                ucmd = "r";
                _console.WriteLine("");
            }
            else
            {
                // Get the rest.
                var res = _console.ReadLine();
                ucmd = res is null ? key.KeyChar.ToString() : key.KeyChar + res;
            }

            if (ucmd is not null)
            {
                // Process the line. Chop up the raw command line into args.
                List<string> args = StringUtils.SplitByToken(ucmd, " ");

                // Process the command and its options.
                bool valid = false;
                if (args.Count > 0)
                {
                    foreach (var cmd in _commands!)
                    {
                        if (args[0] == cmd.LongName || (args[0].Length == 1 && args[0][0] == cmd.ShortName))
                        {
                            // Execute the command. They handle any errors internally.
                            valid = true;

                            ret = cmd.Handler(cmd, args);
                            break;
                        }
                    }

                    if (!valid)
                    {
                        Write("Invalid command");
                    }
                }
            }
            else
            {
                // Assume finished.
                State.Instance.ExecState = ExecState.Exit;
            }

            return ret;
        }

        /// <summary>
        /// Write to user. Takes care of prompt.
        /// </summary>
        /// <param name="s"></param>
        void Write(string s)
        {
            _console.WriteLine(s);
            _console.Write(_prompt);
        }
        #endregion

        #region Command handlers

        //--------------------------------------------------------//
        bool TempoCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            switch (args.Count)
            {
                case 1: // no args - get
                    Write($"{State.Instance.Tempo}");
                    break;

                case 2: // set
                    if (int.TryParse(args[1], out int t) && t >= 40 && t <= 240)
                    {
                        State.Instance.Tempo = t;
                        Write($"tempo set to {t}");
                    }
                    else
                    {
                        ret = false;
                        Write($"invalid tempo: {args[1]}");
                    }
                    break;

                default:
                    ret = false;
                    break;
            }

            return ret;
        }

        //--------------------------------------------------------//
        bool RunCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            switch (args.Count)
            {
                case 1: // no args - get
                    switch (State.Instance.ExecState)
                    {
                        case ExecState.Idle:
                            State.Instance.ExecState = ExecState.Run;
                            Write("running");
                            break;

                        case ExecState.Run:
                            State.Instance.ExecState = ExecState.Idle;
                            Write("stopped");
                            _core.KillAll();
                            break;

                        default:
                            Write("invalid state");
                            ret = false;
                            break;
                    }
                    break;

                default:
                    Write("invalid command");
                    ret = false;
                    break;
            }

            return ret;
        }

        //--------------------------------------------------------//
        bool ExitCmd(CommandDescriptor cmd, List<string> args)
        {
            _core.KillAll();

            State.Instance.ExecState = ExecState.Exit;
            Write($"Goodbye!");

            return true;
        }

        //--------------------------------------------------------//
        bool UsageCmd(CommandDescriptor _, List<string> __)
        {
            // Talk about muself.
            foreach (var cmd in _commands!)
            {
                _console.WriteLine($"{cmd.LongName}|{cmd.ShortName}: {cmd.Info}");
                if (cmd.Args.Length > 0)
                {
                    // Maybe multiline args.
                    var parts = StringUtils.SplitByToken(cmd.Args, Environment.NewLine);
                    foreach (var arg in parts)
                    {
                        _console.WriteLine($"    {arg}");
                    }
                }
            }

            Write("");

            return true;
        }
        #endregion
    }
}
