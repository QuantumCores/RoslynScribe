using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.ScribeConsole
{
    public class SolutionProvider
    {
        public static MSBuildWorkspace GetWorkspace()
        {
            var workspace = MSBuildWorkspace.Create();

            // Print message for WorkspaceFailed event to help diagnosing project load failures.
            workspace.WorkspaceFailed += (o, e) => System.Console.WriteLine(e.Diagnostic.Message);
            workspace.LoadMetadataForReferencedProjects = true;

            return workspace;
        }

        public static async Task<Solution> GetSolution(MSBuildWorkspace workspace, string solutionPath)
        {
            // var solutionPath = "D:\\Source\\TheGame\\TheGame.Town.Api\\TheGame.Town.Api.sln";//args[0];
            //var solutionPath = "D:\\Source\\RoslynScribe\\RoslynScribe.sln";//args[0];
            System.Console.WriteLine($"Loading solution '{solutionPath}'");

            // Attach progress reporter so we print projects as they are loaded.
            var solution = await workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter());

            return solution;
        }
    }
}
