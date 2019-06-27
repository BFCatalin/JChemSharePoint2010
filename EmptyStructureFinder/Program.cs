using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EmptyStructureFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 2 || args.Length == 0)
            {
                Console.WriteLine("Please specify an URL or a file input.");
            }
            else
            {
                LibraryChecker checker = new LibraryChecker();
                if (File.Exists(args[0]))
                {
                    var webUrls = File.ReadAllLines(args[0]);
                    checker.Check(webUrls);
                }
                else
                {
                    checker.Check(args[0]);
                }
            }
        }
    }
}
