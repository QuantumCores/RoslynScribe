using RoslynScribe.Domain.Models;
using System;
using System.IO;
using System.Text.Json;

namespace RoslynScribe.Domain.ScribeConsole
{
    internal static class ResultWriter
    {
        internal static void Write(string outputPath, ScribeResult result)
        {
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, JsonSerializer.Serialize(result));
            Console.WriteLine($"Wrote result to '{outputPath}'", ConsoleColor.Green);
        }
    }
}
