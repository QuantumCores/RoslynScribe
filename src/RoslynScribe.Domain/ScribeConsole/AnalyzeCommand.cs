using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.ScribeConsole
{
    internal static class AnalyzeCommand
    {
        internal static async Task<int> RunAsync(string[] args)
        {
            if (CommandLineHelpers.ContainsHelpFlag(args))
            {
                ConsoleUsage.PrintAnalyze();
                return ConsoleExitCodes.Success;
            }

            if (!TryParseAnalyzeArgs(args, out var options, out var error))
            {
                Console.WriteLine(error, ConsoleColor.Red);
                ConsoleUsage.PrintAnalyze();
                return ConsoleExitCodes.InvalidArgs;
            }

            if (!TryPrepareAnalyzeOptions(options, out error))
            {
                Console.WriteLine(error, ConsoleColor.Red);
                return ConsoleExitCodes.InvalidArgs;
            }

            try
            {
                var result = await AnalyzeAsync(options);
                ResultWriter.Write(options.OutputPath, result);
                return ConsoleExitCodes.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Analyze failed: {ex.Message}", ConsoleColor.Red);
                Console.WriteLine(ex.StackTrace, ConsoleColor.Red);
                return ConsoleExitCodes.AnalyzeFailure;
            }
        }

        internal static async Task<ScribeResult> RunDefaultAsync()
        {
            var slnFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln", SearchOption.TopDirectoryOnly);
            if (slnFiles.Length == 0)
            {
                throw new InvalidOperationException("No solution file found in the current directory. Use the analyze command with -s.");
            }

            if (slnFiles.Length > 1)
            {
                throw new InvalidOperationException("Multiple solution files found in the current directory. Use the analyze command with -s.");
            }

            var options = new AnalyzeOptions { SolutionPath = slnFiles[0] };
            if (!TryPrepareAnalyzeOptions(options, out var error))
            {
                throw new InvalidOperationException(error);
            }

            return await AnalyzeAsync(options);
        }

        private static async Task<ScribeResult> AnalyzeAsync(AnalyzeOptions options)
        {
            Primer.Initialize();

            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message, ConsoleColor.Red);
                workspace.LoadMetadataForReferencedProjects = true;

                Console.WriteLine($"Loading solution '{options.SolutionPath}'", ConsoleColor.Blue);
                var solution = await workspace.OpenSolutionAsync(options.SolutionPath, new ConsoleProgressReporter());
                Console.WriteLine($"Finished loading solution '{options.SolutionPath}'");

                solution = FilterSolution(solution, options);

                var adcConfig = string.IsNullOrWhiteSpace(options.ConfigPath)
                    ? new AdcConfig()
                    : LoadAdcConfig(options.ConfigPath);

                Console.WriteLine($"Analyzing solution '{options.SolutionPath}'", ConsoleColor.Green);
                var nodes = await ScribeAnalyzer.Analyze(workspace, solution, adcConfig);
                Console.WriteLine($"Finished analyzing solution '{options.SolutionPath}'", ConsoleColor.Green);

                Console.WriteLine("Rebuilding nodes", ConsoleColor.Green);
                return ScribeBuilder.Rebuild(nodes);
            }
        }

        private static Solution FilterSolution(Solution solution, AnalyzeOptions options)
        {
            var allProjects = solution.Projects.ToList();
            var includeProjects = new HashSet<string>(options.ProjectNames, StringComparer.OrdinalIgnoreCase);
            var excludeProjects = new HashSet<string>(options.ExcludeProjectNames, StringComparer.OrdinalIgnoreCase);
            var excludePhrases = options.ExcludeProjectPhrases.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            var selectedProjects = allProjects;
            if (includeProjects.Count > 0)
            {
                selectedProjects = allProjects.Where(p => includeProjects.Contains(p.Name)).ToList();
                var missing = includeProjects
                    .Where(name => !selectedProjects.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (missing.Count > 0)
                {
                    Console.WriteLine($"Warning: Project(s) not found: {string.Join(", ", missing)}", ConsoleColor.Yellow);
                }
            }

            var excluded = new List<string>();
            var finalProjects = new List<Project>();
            foreach (var project in selectedProjects)
            {
                if (excludeProjects.Contains(project.Name))
                {
                    excluded.Add(project.Name);
                    continue;
                }

                if (excludePhrases.Any(phrase => project.Name.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    excluded.Add(project.Name);
                    continue;
                }

                finalProjects.Add(project);
            }

            if (excludeProjects.Count > 0)
            {
                var missingExcludes = excludeProjects
                    .Where(name => !allProjects.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (missingExcludes.Count > 0)
                {
                    Console.WriteLine("Warning: Excluded project(s) not found:", ConsoleColor.Yellow);
                    foreach (var proj in missingExcludes)
                    {
                        Console.WriteLine($" - {proj}", ConsoleColor.Yellow);
                    }
                }
            }

            if (excluded.Count > 0)
            {
                Console.WriteLine("Warning: Excluded projects:", ConsoleColor.Yellow);
                foreach (var proj in excluded)
                {
                    Console.WriteLine($" - {proj}", ConsoleColor.Yellow);
                }
            }

            if (finalProjects.Count == 0)
            {
                throw new InvalidOperationException("No projects matched the provided filters.");
            }

            var filteredSolution = solution;
            var keepProjectIds = new HashSet<ProjectId>(finalProjects.Select(p => p.Id));
            foreach (var project in allProjects)
            {
                if (!keepProjectIds.Contains(project.Id))
                {
                    filteredSolution = filteredSolution.RemoveProject(project.Id);
                }
            }

            if (options.Directories.Count == 0 && options.Files.Count == 0)
            {
                return filteredSolution;
            }

            var normalizedDirectories = options.Directories.Select(NormalizeDirectoryPath).ToList();
            var normalizedFiles = new HashSet<string>(options.Files.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
            var unmatchedFiles = new HashSet<string>(normalizedFiles, StringComparer.OrdinalIgnoreCase);

            foreach (var project in filteredSolution.Projects.ToList())
            {
                var documentIdsToRemove = new List<DocumentId>();
                foreach (var document in project.Documents)
                {
                    if (!ShouldIncludeDocument(document.FilePath, normalizedDirectories, normalizedFiles, unmatchedFiles))
                    {
                        documentIdsToRemove.Add(document.Id);
                    }
                }

                for (var i = 0; i < documentIdsToRemove.Count; i++)
                {
                    filteredSolution = filteredSolution.RemoveDocument(documentIdsToRemove[i]);
                }
            }

            if (unmatchedFiles.Count > 0)
            {
                Console.WriteLine($"Warning: File(s) not found in solution: {string.Join(", ", unmatchedFiles)}", ConsoleColor.Yellow);
            }

            var remainingDocuments = filteredSolution.Projects.Sum(p => p.Documents.Count());
            if (remainingDocuments == 0)
            {
                throw new InvalidOperationException("No documents matched the provided filters.");
            }

            return filteredSolution;
        }

        private static bool ShouldIncludeDocument(string filePath, List<string> directories, HashSet<string> files, HashSet<string> unmatchedFiles)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return directories.Count == 0 && files.Count == 0;
            }

            if (directories.Count == 0 && files.Count == 0)
            {
                return true;
            }

            var fullPath = Path.GetFullPath(filePath);
            if (files.Contains(fullPath))
            {
                unmatchedFiles.Remove(fullPath);
                return true;
            }

            for (var i = 0; i < directories.Count; i++)
            {
                if (fullPath.StartsWith(directories[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeDirectoryPath(string directoryPath)
        {
            var fullPath = Path.GetFullPath(directoryPath);
            if (!fullPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) &&
                !fullPath.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                fullPath += Path.DirectorySeparatorChar;
            }

            return fullPath;
        }

        private static bool TryParseAnalyzeArgs(string[] args, out AnalyzeOptions options, out string error)
        {
            options = new AnalyzeOptions();
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
                    case "s":
                    case "p":
                    case "pe":
                    case "pec":
                    case "d":
                    case "f":
                    case "o":
                    case "c":
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
                    case "s":
                        if (!string.IsNullOrWhiteSpace(options.SolutionPath))
                        {
                            error = "Option '-s' can only be specified once.";
                            return false;
                        }
                        options.SolutionPath = value;
                        break;
                    case "p":
                        options.ProjectNames.Add(value);
                        break;
                    case "pe":
                        options.ExcludeProjectNames.Add(value);
                        break;
                    case "pec":
                        options.ExcludeProjectPhrases.Add(value);
                        break;
                    case "d":
                        options.Directories.Add(value);
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
                    case "c":
                        if (!string.IsNullOrWhiteSpace(options.ConfigPath))
                        {
                            error = "Option '-c' can only be specified once.";
                            return false;
                        }
                        options.ConfigPath = value;
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(options.SolutionPath))
            {
                error = "Missing required option '-s'.";
                return false;
            }

            return true;
        }

        private static bool TryPrepareAnalyzeOptions(AnalyzeOptions options, out string error)
        {
            error = null;
            options.SolutionPath = Path.GetFullPath(options.SolutionPath);

            if (!File.Exists(options.SolutionPath))
            {
                error = $"Solution file not found: '{options.SolutionPath}'.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(options.ConfigPath))
            {
                options.ConfigPath = Path.GetFullPath(options.ConfigPath);
                if (!File.Exists(options.ConfigPath))
                {
                    error = $"Config file not found: '{options.ConfigPath}'.";
                    return false;
                }
            }

            if (options.Directories.Count > 0)
            {
                var normalizedDirs = options.Directories.Select(Path.GetFullPath).ToList();
                for (var i = 0; i < normalizedDirs.Count; i++)
                {
                    if (!Directory.Exists(normalizedDirs[i]))
                    {
                        error = $"Directory not found: '{normalizedDirs[i]}'.";
                        return false;
                    }
                }

                options.Directories.Clear();
                options.Directories.AddRange(normalizedDirs);
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
                var solutionName = Path.GetFileNameWithoutExtension(options.SolutionPath);
                options.OutputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{solutionName}.adc.json");
            }
            else
            {
                options.OutputPath = Path.GetFullPath(options.OutputPath);
            }

            return true;
        }

        private static AdcConfig LoadAdcConfig(string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"Warning: Config file '{configPath}' not found. Using empty config.", ConsoleColor.Yellow);
                    return new AdcConfig();
                }

                var json = File.ReadAllText(configPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine($"Warning: Config file '{configPath}' is empty. Using empty config.", ConsoleColor.Yellow);
                    return new AdcConfig();
                }

                var config = JsonSerializer.Deserialize<AdcConfig>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (config == null)
                {
                    Console.WriteLine($"Warning: Config file '{configPath}' is invalid. Using empty config.", ConsoleColor.Yellow);
                    return new AdcConfig();
                }

                config.FlattenMethodOverrides();

                return config;
            }
            catch
            {
                Console.WriteLine($"Warning: Failed to read config file '{configPath}'. Using empty config.", ConsoleColor.Yellow);
                return new AdcConfig();
            }
        }

        private sealed class AnalyzeOptions
        {
            public string SolutionPath { get; set; }
            public List<string> ProjectNames { get; } = new List<string>();
            public List<string> ExcludeProjectNames { get; } = new List<string>();
            public List<string> ExcludeProjectPhrases { get; } = new List<string>();
            public List<string> Directories { get; } = new List<string>();
            public List<string> Files { get; } = new List<string>();
            public string OutputPath { get; set; }
            public string ConfigPath { get; set; }
        }
    }
}
