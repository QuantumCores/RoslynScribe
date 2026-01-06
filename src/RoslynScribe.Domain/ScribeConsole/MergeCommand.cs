using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RoslynScribe.Domain.ScribeConsole
{
    internal static class MergeCommand
    {
        internal static int Run(string[] args)
        {
            if (CommandLineHelpers.ContainsHelpFlag(args))
            {
                ConsoleUsage.PrintMerge();
                return ConsoleExitCodes.Success;
            }

            if (!TryParseMergeArgs(args, out var options, out var error))
            {
                Console.WriteLine(error, ConsoleColor.Red);
                ConsoleUsage.PrintMerge();
                return ConsoleExitCodes.InvalidArgs;
            }

            if (!TryPrepareMergeOptions(options, out error))
            {
                Console.WriteLine(error, ConsoleColor.Red);
                return ConsoleExitCodes.InvalidArgs;
            }

            try
            {
                MergeResults(options);
                return ConsoleExitCodes.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Merge failed: {ex.Message}", ConsoleColor.Red);
                return ConsoleExitCodes.MergeFailure;
            }
        }

        private static void MergeResults(MergeOptions options)
        {
            var resultFiles = new List<string>();
            if (!string.IsNullOrWhiteSpace(options.DirectoryPath))
            {
                resultFiles.AddRange(Directory.GetFiles(options.DirectoryPath, "*.adc.json", SearchOption.TopDirectoryOnly));
            }
            else
            {
                resultFiles.AddRange(options.Files);
            }

            if (resultFiles.Count == 0)
            {
                throw new InvalidOperationException("No result files found to merge.");
            }

            var results = new List<ScribeResult>();
            for (var i = 0; i < resultFiles.Count; i++)
            {
                var filePath = resultFiles[i];
                try
                {
                    var json = File.ReadAllText(filePath);
                    var result = JsonSerializer.Deserialize<ScribeResult>(json);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: '{filePath}' contains no data. Skipping.", ConsoleColor.Yellow);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to read '{filePath}': {ex.Message}. Skipping.", ConsoleColor.Yellow);
                }
            }

            if (results.Count == 0)
            {
                throw new InvalidOperationException("No valid result files found to merge.");
            }

            var merged = ScribeBuilder.Merge(results.ToArray());
            ResultWriter.Write(options.OutputPath, merged);
        }

        private static bool TryParseMergeArgs(string[] args, out MergeOptions options, out string error)
        {
            options = new MergeOptions();
            error = null;

            if (args == null)
            {
                error = "No arguments provided.";
                return false;
            }

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (!arg.StartsWith("-", StringComparison.Ordinal))
                {
                    error = $"Unexpected argument '{arg}'.";
                    return false;
                }

                var option = CommandLineHelpers.NormalizeOption(arg);
                if (string.IsNullOrWhiteSpace(option))
                {
                    error = $"Invalid option '{arg}'.";
                    return false;
                }

                switch (option)
                {
                    case "d":
                    case "f":
                    case "o":
                        break;
                    default:
                        error = $"Unknown option '{arg}'.";
                        return false;
                }

                if (!CommandLineHelpers.TryReadOptionValue(args, ref i, out var value, out error))
                {
                    return false;
                }

                switch (option)
                {
                    case "d":
                        if (!string.IsNullOrWhiteSpace(options.DirectoryPath))
                        {
                            error = "Option '-d' can only be specified once.";
                            return false;
                        }
                        options.DirectoryPath = value;
                        break;
                    case "f":
                        options.Files.Add(value);
                        break;
                    case "o":
                        if (!string.IsNullOrWhiteSpace(options.OutputPath))
                        {
                            error = "Option '-o' can only be specified once.";
                            return false;
                        }
                        options.OutputPath = value;
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(options.DirectoryPath) && options.Files.Count > 0)
            {
                error = "Options '-d' and '-f' cannot be used together.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.DirectoryPath) && options.Files.Count == 0)
            {
                error = "Either '-d' or '-f' must be provided.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.DirectoryPath) && options.Files.Count < 2)
            {
                error = "At least two '-f' paths are required when '-d' is not used.";
                return false;
            }

            return true;
        }

        private static bool TryPrepareMergeOptions(MergeOptions options, out string error)
        {
            error = null;

            if (!string.IsNullOrWhiteSpace(options.DirectoryPath))
            {
                options.DirectoryPath = Path.GetFullPath(options.DirectoryPath);
                if (!Directory.Exists(options.DirectoryPath))
                {
                    error = $"Directory not found: '{options.DirectoryPath}'.";
                    return false;
                }
            }

            if (options.Files.Count > 0)
            {
                var normalizedFiles = options.Files.Select(Path.GetFullPath).ToList();
                for (var i = 0; i < normalizedFiles.Count; i++)
                {
                    if (!File.Exists(normalizedFiles[i]))
                    {
                        error = $"File not found: '{normalizedFiles[i]}'.";
                        return false;
                    }
                }

                options.Files.Clear();
                options.Files.AddRange(normalizedFiles);
            }

            if (string.IsNullOrWhiteSpace(options.OutputPath))
            {
                options.OutputPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    $"merge_{DateTime.Now:yyyy_MM_dd}.adc.json");
            }
            else
            {
                options.OutputPath = Path.GetFullPath(options.OutputPath);
            }

            return true;
        }

        private sealed class MergeOptions
        {
            public string DirectoryPath { get; set; }
            public List<string> Files { get; } = new List<string>();
            public string OutputPath { get; set; }
        }
    }
}
