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
        }
    }
}