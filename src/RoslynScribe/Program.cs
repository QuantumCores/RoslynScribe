using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoslynScribe
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Primer.Initialize();

            using (var workspace = MSBuildWorkspace.Create())
            {
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
                workspace.LoadMetadataForReferencedProjects = true;

                // var solutionPath = "D:\\Source\\TheGame\\TheGame.Town.Api\\TheGame.Town.Api.sln";//args[0];
                var solutionPath = "D:\\Source\\Scribe\\RoslynScribe\\RoslynScribe.sln";//args[0];
                Console.WriteLine($"Loading solution '{solutionPath}'");

                // Attach progress reporter so we print projects as they are loaded.
                var solution = await workspace.OpenSolutionAsync(solutionPath, new ConsoleProgressReporter());

                await ScribeAnalyze(workspace, solution);

                Console.WriteLine($"Finished loading solution '{solutionPath}'");

                // TODO: Do analysis on the projects in the loaded solution
            }
        }

        private static async Task ScribeAnalyze(MSBuildWorkspace workspace, Solution solution)
        {
            var result = new List<ScribeNode>();
            var keeper = GetKeeper(solution);

            foreach (var project in solution.Projects)
            {
                // var compilation = await project.GetCompilationAsync();
                if (project.Name == "RoslynScribe.TestProject")
                {
                    foreach (var document in project.Documents)
                    {
                        if (document.Name == project.Name + ".AssemblyInfo.cs" ||
                            document.Name == project.Name + ".GlobalUsings.g.cs")
                        {
                            continue;
                        }

                        Console.WriteLine($"{document.Name}");
                        var semanticModel = await document.GetSemanticModelAsync();
                        var tree = await document.GetSyntaxTreeAsync();
                        var rootNode = await tree.GetRootAsync();

                        var scribeNode = new ScribeNode() { Kind = document.Name };
                        result.Add(scribeNode);
                        Console.WriteLine(scribeNode);

                        Traverse(rootNode, scribeNode, semanticModel, keeper);
                        //SyntaxTreePrinter.Print(rootNode);
                        ScribeTreePrinter.Print(scribeNode);
                    }
                }

                var diagnostics = workspace.Diagnostics;
                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine(diagnostic.Message);
                }
            }
        }

        private static Dictionary<string, Dictionary<string, Document>> GetKeeper(Solution solution)
        {
            var keeper = new Dictionary<string, Dictionary<string, Document>>();

            foreach (var project in solution.Projects)
            {
                var documentKeeper = new Dictionary<string, Document>();
                keeper.Add(project.AssemblyName, documentKeeper);

                if (project.Name == "RoslynScribe.TestProject")
                {
                    foreach (var document in project.Documents)
                    {
                        documentKeeper.Add(document.Name, document);
                    }
                }
            }

            return keeper;
        }

        private static void Traverse(SyntaxNode node, ScribeNode parentNode, SemanticModel semanticModel, Dictionary<string, Dictionary<string, Document>> keeper)
        {
            var nodes = node.ChildNodes();
            foreach (var syntaxNode in nodes)
            {
                var kind = syntaxNode.Kind();
                if (KindToSkip(kind))
                {
                    continue;
                }

                ProcessNode(parentNode, semanticModel, keeper, syntaxNode, kind);
            }
        }

        private static void ProcessNode(ScribeNode parentNode, SemanticModel semanticModel, Dictionary<string, Dictionary<string, Document>> keeper, SyntaxNode syntaxNode, SyntaxKind kind)
        {
            var lTrivias = syntaxNode.GetLeadingTrivia();
            var childNode = FindCommentTrivia(syntaxNode, parentNode, lTrivias);

            if (kind == SyntaxKind.InvocationExpression)
            {
                var invokedMethod = GetMethodInfo(syntaxNode as InvocationExpressionSyntax, semanticModel);
                if (invokedMethod != null)
                {
                    var syntaxReferences = invokedMethod.DeclaringSyntaxReferences;
                    foreach (var syntaxReference in syntaxReferences)
                    {
                        var methodNode = syntaxReference.GetSyntax();
                        ProcessNode(childNode ?? parentNode, semanticModel, keeper, methodNode, methodNode.Kind());
                    }
                }
            }
            //var tTrivias = syntaxNode.GetTrailingTrivia();
            //FindCommentTrivia(syntaxNode, parentNode, tTrivias);

            if (KindToNotTraverse(kind))
            {
                return;
            }

            Traverse(syntaxNode, childNode ?? parentNode, semanticModel, keeper);
        }

        private static bool IsCommentType(SyntaxKind kind)
        {
            return kind == SyntaxKind.SingleLineCommentTrivia
                || kind == SyntaxKind.MultiLineCommentTrivia;
        }

        private static ScribeNode FindCommentTrivia(SyntaxNode syntaxNode, ScribeNode parentNode, SyntaxTriviaList syntaxTrivias)
        {
            var comments = new List<string>();
            foreach (var trivia in syntaxTrivias)
            {
                if (trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                {
                    comments.Add(trivia.ToString());
                }
            }

            if (comments.Count != 0)
            {
                return AddChildNode(syntaxNode, parentNode, comments.ToArray());
            }

            return null;
        }

        private static bool KindToSkip(SyntaxKind kind)
        {
            return kind == SyntaxKind.Attribute ||
                kind == SyntaxKind.AttributeArgument ||
                kind == SyntaxKind.AttributeArgumentList ||
                kind == SyntaxKind.AttributeList ||
                kind == SyntaxKind.AttributeTargetSpecifier;
        }

        private static bool KindToNotTraverse(SyntaxKind kind)
        {
            return kind == SyntaxKind.LocalDeclarationStatement;
        }

        private static ScribeNode AddChildNode(SyntaxNode syntaxNode, ScribeNode parentNode, string[] value)
        {
            var childNode = new ScribeNode { Kind = syntaxNode.Kind().ToString(), Value = value };
            parentNode.ChildNodes.Add(childNode);
            Console.WriteLine(childNode);
            return childNode;
        }

        private static ISymbol GetMethodInfo(InvocationExpressionSyntax syntaxNode, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            return methodSymbol;
        }
    }
}
