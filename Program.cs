using System;

namespace NTerm
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            // var app = new AsyncTest();
            var app = new App();
            app.Dispose();
            //app.Run();
        }
    }
}
