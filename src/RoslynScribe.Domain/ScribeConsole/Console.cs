using System;

namespace RoslynScribe.Domain.ScribeConsole
{
    public static class Console
    {
        public static void WriteLine(string message, ConsoleColor? color = null)
        {
            var previousColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = color ?? previousColor;
            System.Console.WriteLine(message);
            System.Console.ForegroundColor = previousColor;
        }
    }
}
