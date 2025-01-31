using System;

namespace NTerm
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var app = new App();
            app.Run();
            app.Dispose();


            //// Process cmd line args.
            //switch (args.Length)
            //{
            //    case 0:
            //        //ApplicationConfiguration.Initialize();
            //        //Application.Run(new MainForm());
            //        break;

            //    case 1:
            //        var scriptFn = args[0];
            //        //RealConsole console = new();
            //        var app = new App();
            //        app.Run();
            //        app.Dispose();
            //        break;

            //    default:
            //        Console.WriteLine("Invalid command line");
            //        break;
            //}
        }
    }
}