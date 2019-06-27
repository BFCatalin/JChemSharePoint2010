using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JChemSharePointEmptyStructureFinder
{
    class Options
    {
        [Option('u', Required = false, SetName = "Source",
            HelpText = "Site URL to look into. All of the libraries will be parse and check for structures that are not linked.")]
        public string Url { get; set; }

        [Option('i', Required = false, SetName = "Source",
            HelpText = "A file that contains site URLs on each line. The tool will use these URLs to check for structures that are not linked.")]
        public string Input { get; set; }
    }
}
