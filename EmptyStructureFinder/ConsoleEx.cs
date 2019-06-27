using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EmptyStructureFinder
{
    public static class ConsoleEx
    {
        public static void WriteLine(ConsoleColor foregroundColor, string format, params object[] arg)
        {
            var foreColor = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;
            Console.WriteLine(format, arg);
            Console.ForegroundColor = foreColor;
        }

        public static void WriteLine(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }
    }
}
