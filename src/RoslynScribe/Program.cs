using Microsoft.CodeAnalysis.MSBuild;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.ScribeConsole;
using RoslynScribe.Domain.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoslynScribe
{
    internal class Program
    {
        private const string ADCConfigFileName = "adc.config.json";

        static async Task Main(string[] args)
        {
            await Run();
        }

        internal static async Task<ScribeResult> Run()
        {
            Primer.Initialize();

            var nodes = new List<ScribeNode>();
            using (var workspace = MSBuildWorkspace.Create())
            {
                Domain.ScribeConsole.Console.WriteLine($"Reading configuration from '{ADCConfigFileName}' if available", ConsoleColor.Green);
                var adcConfig = LoadAdcConfig(Path.Combine(AppContext.BaseDirectory, ADCConfigFileName));

                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => Domain.ScribeConsole.Console.WriteLine(e.Diagnostic.Message, ConsoleColor.Red);
                workspace.LoadMetadataForReferencedProjects = true;

                // var solutionPath = "D:\\Source\\TheGame\\TheGame.Town.Api\\TheGame.Town.Api.sln";//args[0];
                var solutionPath = "D:\\Source\\RoslynScribe\\RoslynScribe.sln";//args[0];
                Domain.ScribeConsole.Console.WriteLine($"Loading solution '{solutionPath}'", ConsoleColor.Blue);

                // Attach progress reporter so we print projects as they are loaded.                
                var solution = await workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter());
                Domain.ScribeConsole.Console.WriteLine($"Finished loading solution '{solutionPath}'");


                Domain.ScribeConsole.Console.WriteLine($"Analyzing solution '{solutionPath}'", ConsoleColor.Green);
                nodes = await ScribeAnalyzer.Analyze(workspace, solution, adcConfig);

                Domain.ScribeConsole.Console.WriteLine($"Finished analyzing solution '{solutionPath}'", ConsoleColor.Green);
            }

            Domain.ScribeConsole.Console.WriteLine($"Rebuilding nodes", ConsoleColor.Green);
            var result = ScribeBuilder.Rebuild(nodes);

            File.WriteAllText("result.json", JsonSerializer.Serialize(result));

            return result;
        }

        private static AdcConfig LoadAdcConfig(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    return CreateEmptyAdcConfig();
                }

                var json = File.ReadAllText(configPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return CreateEmptyAdcConfig();
                }

                var config = JsonSerializer.Deserialize<AdcConfig>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return config ?? CreateEmptyAdcConfig();
            }
            catch
            {
                // Treat unreadable/invalid configs as "no config"
                return CreateEmptyAdcConfig();
            }
        }

        private static AdcConfig CreateEmptyAdcConfig()
        {
            return new AdcConfig();
        }
    }
}
