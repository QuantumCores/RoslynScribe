using Microsoft.CodeAnalysis.MSBuild;
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
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
                workspace.LoadMetadataForReferencedProjects = true;

                // var solutionPath = "D:\\Source\\TheGame\\TheGame.Town.Api\\TheGame.Town.Api.sln";//args[0];
                var solutionPath = "D:\\Source\\RoslynScribe\\RoslynScribe.sln";//args[0];
                Console.WriteLine($"Loading solution '{solutionPath}'");

                // Attach progress reporter so we print projects as they are loaded.
                var solution = await workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter());
                Console.WriteLine($"Finished loading solution '{solutionPath}', analyzing...");

                Console.WriteLine($"Analyzing solution '{solutionPath}'");
                nodes = await ScribeAnalyzer.Analyze(workspace, solution);
                Console.WriteLine($"Finished analyzing solution '{solutionPath}', rebuilding...");
                
            }

            Console.WriteLine($"Rebuilding nodes");
            var result = ScribeAnalyzer.Rebuild(nodes);

            File.WriteAllText("result.json", JsonSerializer.Serialize(result));

            return result;
        }
    }
}
