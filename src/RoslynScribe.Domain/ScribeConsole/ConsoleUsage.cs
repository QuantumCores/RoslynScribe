using System;

namespace RoslynScribe.Domain.ScribeConsole
{
    public static class ConsoleUsage
    {
        public static void PrintGeneral()
        {
            Console.WriteLine("Usage:", ConsoleColor.Green);
            Console.WriteLine("  RoslynScribe analyze -s <solution> [options]");
            Console.WriteLine("  RoslynScribe merge -d <directory> [options]");
            Console.WriteLine("  RoslynScribe merge -f <file> -f <file> [options]");
            Console.WriteLine(string.Empty);
            Console.WriteLine("Use 'RoslynScribe analyze --help' or 'RoslynScribe merge --help' for details.");
        }

        public static void PrintAnalyze()
        {
            Console.WriteLine("Analyze usage:", ConsoleColor.Green);
            Console.WriteLine("  RoslynScribe analyze -s <solution> [options]");
            Console.WriteLine(string.Empty);
            Console.WriteLine("Options:");
            Console.WriteLine("  -s, --solution <path>                    Solution path (required)");
            Console.WriteLine("  -p, --project <name>                     Project name to include (repeatable)");
            Console.WriteLine("  -pe, --project-exclude <name>            Project name to exclude (repeatable)");
            Console.WriteLine("  -pec, --project-exclude-contains <text>  Exclude projects containing phrase (repeatable)");
            Console.WriteLine("  -d, --dir <path>                         Directory to analyze (repeatable)");
            Console.WriteLine("  -f, --file <path>                        File to analyze (repeatable)");
            Console.WriteLine("  -o, --output <path>                      Output file path");
            Console.WriteLine("  -c, --config <path>                      ADC config path");
            Console.WriteLine(string.Empty);
            Console.WriteLine("Notes:");
            Console.WriteLine("  - Directories and files are combined (union) when both are provided.");
        }

        public static void PrintMerge()
        {
            Console.WriteLine("Merge usage:", ConsoleColor.Green);
            Console.WriteLine("  RoslynScribe merge -d <directory> [options]");
            Console.WriteLine("  RoslynScribe merge -f <file> -f <file> [options]");
            Console.WriteLine(string.Empty);
            Console.WriteLine("Options:");
            Console.WriteLine("  -d, --dir <path>      Directory containing *.adc.json files");
            Console.WriteLine("  -f, --file <path>     File to merge (repeatable, at least two when -d is not used)");
            Console.WriteLine("  -o, --output <path>   Output file path");
        }
    }
}
