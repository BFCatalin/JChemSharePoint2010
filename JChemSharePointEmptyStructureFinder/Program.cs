using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JChemSharePointEmptyStructureFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(opts => Run(opts));
        }

        private static void Run(Options opts)
        {
            var libraryChecker = new LibraryChecker();
            if (!string.IsNullOrEmpty(opts.Url) && !string.IsNullOrEmpty(opts.Input))
            {
                Console.WriteLine("Please specify an URL or a file input.");
            }
            else if (!string.IsNullOrEmpty(opts.Url))
            {
                libraryChecker.Check(opts.Url);
            }
            else if (!string.IsNullOrEmpty(opts.Input))
            {
                libraryChecker.Check(opts.Input);
            }
            else
            {
                Console.WriteLine("Please specify an URL or a file input.");
            }
        }
    }
}
