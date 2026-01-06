using RoslynScribe.Domain.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.ScribeConsole
{
    public static class ScribeConsoleApp
    {
        public static async Task<int> Run(string[] args)
        {
            try
            {
                if (args == null || args.Length == 0)
                {
                    ConsoleUsage.PrintGeneral();
                    return ConsoleExitCodes.InvalidArgs;
                }

                var command = args[0];
                if (CommandLineHelpers.IsHelpCommand(command))
                {
                    ConsoleUsage.PrintGeneral();
                    return ConsoleExitCodes.Success;
                }

                var remaining = args.Skip(1).ToArray();
                if (command.Equals("analyze", StringComparison.OrdinalIgnoreCase))
                {
                    return await AnalyzeCommand.RunAsync(remaining);
                }

                if (command.Equals("merge", StringComparison.OrdinalIgnoreCase))
                {
                    return MergeCommand.Run(remaining);
                }

                Console.WriteLine($"Unknown command '{command}'.", ConsoleColor.Red);
                ConsoleUsage.PrintGeneral();
                return ConsoleExitCodes.InvalidArgs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error: {ex.Message}", ConsoleColor.Red);
                Console.WriteLine(ex.StackTrace, ConsoleColor.Red);
                return 1;
            }
        }

        public static Task<ScribeResult> RunDefaultAsync()
        {
            return AnalyzeCommand.RunDefaultAsync();
        }
    }
}
