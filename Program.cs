using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ephemera.NBagOfTricks;


namespace NTerm
{
    internal class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using var app = new App([.. args]);
        }
    }
}
