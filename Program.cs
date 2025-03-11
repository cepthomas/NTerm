using System;
using System.Windows.Forms;

namespace NTerm
{
    internal static class Program  
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary> 
        [STAThread]
        static void Main()
        {
            // var app = new App();
            // app.Run();
            // app.Dispose();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}