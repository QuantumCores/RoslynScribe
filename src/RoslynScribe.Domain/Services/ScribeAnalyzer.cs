using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using RoslynScribe.Domain.Extensions;
using RoslynScribe.Domain.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.Services
{
    public static class ScribeAnalyzer
    {
        public const string CommentLabel = "[ADC]";
        public const string GuidesLabel = "[ADG]";

        private static readonly string[] Starts = { $"//{CommentLabel}", $"// {CommentLabel}" };

        public static async Task<List<ScribeNode>> Analyze(MSBuildWorkspace workspace, Solution solution)
        {
            var documents = solution.Projects.SelectMany(x => x.Documents).ToDictionary(x => x.FilePath);
            var result = new List<ScribeNode>();

            foreach (var project in solution.Projects)
            {
                if (project.Name == "RoslynScribe.TestProject")
                {
                    foreach (var document in project.Documents)
                    {
                        var node = await Analyze(solution, project, documents, document);
                        if (node != null)
                        {
                            result.Add(node);
                        }
                    }
                }

                var diagnostics = workspace.Diagnostics;
                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine(diagnostic.Message);
                }
            }

            return result;
        }

        internal static Task<ScribeNode> Analyze(Solution solution, string projectName, string documentName)
        {
            var project = solution.Projects.Single(x => x.Name == projectName);
            var documents = solution.Projects.SelectMany(x => x.Documents).ToDictionary(x => x.FilePath);
            var document = project.Documents.Single(x => x.Name == documentName);
            return Analyze(solution, project, documents, document);
        }

        private static async Task<ScribeNode> Analyze(
            Solution solution,
            Project project,
            Dictionary<string, Document> documents,
            Document document)
        {
            if (document.Name == project.Name + ".AssemblyInfo.cs" ||
                                document.Name == project.Name + ".GlobalUsings.g.cs" ||
                                document.Name.EndsWith("AssemblyAttributes.cs"))
            {
                return null;
            }

            // Console.WriteLine($"{document.Name}");
            var semanticModel = await document.GetSemanticModelAsync();
            var tree = await document.GetSyntaxTreeAsync();
            var rootNode = await tree.GetRootAsync();

            var scribeNode = new ScribeNode()
            {
                Id = Guid.NewGuid(),
                Kind = "Document",
                MetaInfo = new MetaInfo
                {
                    ProjectName = project.Name,
                    DocumentName = document.Name,
                    DocumentPath = document.FilePath,
                    Identifier = document.FilePath
                }
            };

            Traverse(rootNode, scribeNode, semanticModel, documents);

            // SyntaxTreePrinter.Print(rootNode);            
            // ScribeTreePrinter.Print(scribeNode);
            // Console.WriteLine();
            // Console.WriteLine();

            return scribeNode;
        }

        public static ScribeResult Rebuild(List<ScribeNode> nodes)
        {
            var dictionary = RegisterNodes(nodes);
            return Rebuild(nodes, dictionary);
        }

        private static ScribeResult Rebuild(List<ScribeNode> nodes, Dictionary<int, ScribeNode> dictionary)
        {
            var result = new Dictionary<Guid, ScribeNode>();
            if (dictionary.Count != 0)
            {
                foreach (var node in nodes)
                {
                    Rebuild(node, dictionary, result);
                }
            }

            return new ScribeResult() { Nodes = result, Trees = nodes };
        }

        private static ScribeNode Rebuild(ScribeNode node, Dictionary<int, ScribeNode> dictionary, Dictionary<Guid, ScribeNode> result)
        {
            if (dictionary.TryGetValue(node.MetaInfo.GetHashCode(), out var reference))
            {
                if (!result.ContainsKey(reference.Id))
                {
                    result.Add(reference.Id, node);
                }
                else
                {
                    return new ScribeNode { Id = node.Id, TargetNodeId = reference.Id };
                }
            }

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                node.ChildNodes[i] = Rebuild(node.ChildNodes[i], dictionary, result);
            }

            return node;
        }

        internal static Dictionary<int, ScribeNode> RegisterNodes(List<ScribeNode> nodes)
        {
            var result = new Dictionary<int, NodeCounter>();
            foreach (var node in nodes)
            {
                RegisterNode(node, result);
            }

            return result.Where(x => x.Value.Count > 1).ToDictionary(x => x.Key, x => x.Value.Node);
        }

        private static void RegisterNode(ScribeNode node, Dictionary<int, NodeCounter> dictionary)
        {
            var key = node.MetaInfo.GetHashCode();            
            if (dictionary.TryGetValue(key, out var registered))
            {
                registered.Count++;
            }
            else
            {
                dictionary.Add(key, new NodeCounter(node));                
            }

            foreach (var child in node.ChildNodes)
            {
                RegisterNode(child, dictionary);
            }
        }

        private static void ProcessNode(SyntaxNode syntaxNode, SyntaxKind syntaxKind, ScribeNode parentNode, SemanticModel semanticModel, Dictionary<string, Document> documents)
        {
            SetMetaInfo(syntaxNode, syntaxKind, parentNode);
            var lTrivias = syntaxNode.GetLeadingTrivia();
            var childNode = FindCommentTrivia(syntaxNode, syntaxKind, parentNode, lTrivias, semanticModel);

            if (syntaxKind == SyntaxKind.InvocationExpression)
            {
                var invokedMethod = GetMethodInfo(syntaxNode as InvocationExpressionSyntax, semanticModel);
                if (invokedMethod != null)
                {
                    var syntaxReferences = invokedMethod.DeclaringSyntaxReferences;
                    for (int i = 0; i < syntaxReferences.Length; i++)
                    {
                        var location = invokedMethod.Locations[i].GetLineSpan().Path;
                        var isLocal = location == parentNode.MetaInfo.DocumentPath;
                        var contextSemanticModel = isLocal ? semanticModel : documents[location].GetSemanticModelAsync().Result;
                        var syntaxReference = syntaxReferences[i];
                        var methodNode = syntaxReference.GetSyntax();
                        ProcessNode(methodNode, methodNode.Kind(), childNode ?? parentNode, contextSemanticModel, documents);
                    }
                }
            }
            //var tTrivias = syntaxNode.GetTrailingTrivia();
            //FindCommentTrivia(syntaxNode, parentNode, tTrivias);

            if (KindSkipTraverse(syntaxNode, syntaxKind))
            {
                return;
            }

            Traverse(syntaxNode, childNode ?? parentNode, semanticModel, documents);
        }

        private static void Traverse(SyntaxNode node, ScribeNode parentNode, SemanticModel semanticModel, Dictionary<string, Document> documents)
        {
            var nodes = node.ChildNodes();
            foreach (var syntaxNode in nodes)
            {
                var kind = syntaxNode.Kind();
                if (KindToSkip(kind))
                {
                    continue;
                }

                ProcessNode(syntaxNode, kind, parentNode, semanticModel, documents);
            }
        }

        private static ScribeNode FindCommentTrivia(SyntaxNode syntaxNode, SyntaxKind syntaxKind, ScribeNode parentNode, SyntaxTriviaList syntaxTrivias, SemanticModel semanticModel)
        {
            var line = -1;
            var comments = new List<string>();
            foreach (var trivia in syntaxTrivias)
            {
                var tmp = trivia.ToString();
                if ((trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)) && Starts.Any(x => tmp.StartsWith(x)))
                {
                    comments.Add(tmp);
                    if (line == -1)
                    {
                        line = trivia.GetLocation().GetLineSpan().Span.Start.Line;
                    }
                }
            }

            if (comments.Count != 0)
            {
                return AddChildNode(syntaxNode, syntaxKind, parentNode, comments.ToArray(), semanticModel, line);
            }

            return null;
        }

        private static bool IsCommentType(SyntaxKind kind)
        {
            return kind == SyntaxKind.SingleLineCommentTrivia
                || kind == SyntaxKind.MultiLineCommentTrivia;
        }

        private static bool KindToSkip(SyntaxKind kind)
        {
            return kind == SyntaxKind.Attribute ||
                kind == SyntaxKind.AttributeArgument ||
                kind == SyntaxKind.AttributeArgumentList ||
                kind == SyntaxKind.AttributeList ||
                kind == SyntaxKind.AttributeTargetSpecifier ||
                kind == SyntaxKind.CompilationUnit;
        }

        private static bool KindSkipTraverse(SyntaxNode syntaxNode, SyntaxKind kind)
        {
            return kind == SyntaxKind.ExpressionStatement ||
                kind == SyntaxKind.LocalDeclarationStatement && !IsMultiline(syntaxNode);
        }

        private static bool IsMultiline(SyntaxNode syntaxNode)
        {
            var text = syntaxNode.GetText();

            var start = 0;
            foreach (var line in text.Lines)
            {
                if (!line.ToString().Trim().StartsWith("//"))
                {
                    break;
                }
                start++;
            }

            var stop = text.Lines.Count - 1;
            for (int i = text.Lines.Count - 1; i >= 0; i--)
            {
                var line = text.Lines[i];
                if (!string.IsNullOrWhiteSpace(line.ToString()))
                {
                    break;
                }
                stop--;
            }

            return start != stop;
        }

        private static ScribeNode AddChildNode(SyntaxNode syntaxNode, SyntaxKind syntaxKind, ScribeNode parentNode, string[] value, SemanticModel semanticModel, int line)
        {
            var childNode = new ScribeNode
            {
                Id = Guid.NewGuid(),
                //ParentNode = parentNode,
                MetaInfo = GetMetaInfo(syntaxNode, syntaxKind, parentNode, semanticModel, line),
                Kind = syntaxKind.ToString(),
                Value = value,
                Comment = ScribeCommnetParser.Parse(value),
            };

            parentNode.ChildNodes.Add(childNode);

            return childNode;
        }

        private static void SetMetaInfo(SyntaxNode syntaxNode, SyntaxKind syntaxKind, ScribeNode parentNode)
        {
            switch (syntaxKind)
            {
                case SyntaxKind.NamespaceDeclaration:
                    var namespaceSyntax = (syntaxNode as NamespaceDeclarationSyntax);

                    if (parentNode.MetaInfo.NameSpace != null)
                    {
                        return;
                    }
                    var metaInfo = parentNode.MetaInfo;
                    metaInfo.NameSpace = namespaceSyntax.Name.ToString();
                    parentNode.MetaInfo = metaInfo;
                    break;
                case SyntaxKind.ClassDeclaration:
                    var classSyntax = (syntaxNode as ClassDeclarationSyntax);

                    if (parentNode.MetaInfo.TypeName != null)
                    {
                        return;
                    }
                    var classMetaInfo = parentNode.MetaInfo;
                    classMetaInfo.TypeName = classSyntax.Identifier.ValueText;
                    parentNode.MetaInfo = classMetaInfo;
                    break;
                default:
                    break;
            }
        }

        private static ISymbol GetMethodInfo(InvocationExpressionSyntax syntaxNode, SemanticModel semanticModel)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            return methodSymbol;
        }

        private static MetaInfo GetMetaInfo(SyntaxNode syntaxNode, SyntaxKind syntaxKind, ScribeNode parentNode, SemanticModel semanticModel, int line)
        {
            var metaInfo = new MetaInfo();
            metaInfo.Line = line;

            switch (syntaxKind)
            {
                case SyntaxKind.NamespaceDeclaration:
                    var namespaceSyntax = (syntaxNode as NamespaceDeclarationSyntax);

                    metaInfo.ProjectName = parentNode.MetaInfo.ProjectName;
                    metaInfo.DocumentName = parentNode.MetaInfo.DocumentName;
                    metaInfo.DocumentPath = parentNode.MetaInfo.DocumentPath;

                    metaInfo.NameSpace = namespaceSyntax.Name.ToString();
                    metaInfo.Identifier = namespaceSyntax.Name.ToString();
                    break;
                case SyntaxKind.ClassDeclaration:
                    var classSyntax = (syntaxNode as ClassDeclarationSyntax);

                    metaInfo.ProjectName = parentNode.MetaInfo.ProjectName;
                    metaInfo.DocumentName = parentNode.MetaInfo.DocumentName;
                    metaInfo.DocumentPath = parentNode.MetaInfo.DocumentPath;
                    metaInfo.NameSpace = parentNode.MetaInfo.NameSpace;

                    metaInfo.TypeName = classSyntax.Identifier.ValueText;
                    metaInfo.Identifier = classSyntax.Identifier.ValueText;
                    break;
                case SyntaxKind.MethodDeclaration:
                    var methodSyntax = (syntaxNode as MethodDeclarationSyntax);

                    // methods can be called from other project thus copying data from parent won't work
                    var symbolInfo = semanticModel.GetDeclaredSymbol(methodSyntax);
                    return GetMetaInfo(symbolInfo, line);
                default:
                    metaInfo.ProjectName = parentNode.MetaInfo.ProjectName;
                    metaInfo.DocumentName = parentNode.MetaInfo.DocumentName;
                    metaInfo.DocumentPath = parentNode.MetaInfo.DocumentPath;
                    metaInfo.NameSpace = parentNode.MetaInfo.NameSpace;
                    metaInfo.TypeName = parentNode.MetaInfo.TypeName;
                    metaInfo.MemberName = parentNode.MetaInfo.MemberName;
                    break;
            }

            return metaInfo;
        }

        private static MetaInfo GetMetaInfo(IMethodSymbol symbolInfo, int line)
        {
            var metaInfo = new MetaInfo();
            metaInfo.ProjectName = symbolInfo.ContainingAssembly.Name;

            var location = symbolInfo.Locations[0].GetLineSpan().Path;
            metaInfo.DocumentName = Path.GetFileName(location);
            metaInfo.DocumentPath = location;
            metaInfo.NameSpace = symbolInfo.ContainingNamespace.ToString();
            metaInfo.TypeName = symbolInfo.ContainingType.Name;
            metaInfo.MemberName = symbolInfo.Name;
            metaInfo.Identifier = symbolInfo.OriginalDefinition.ToString();
            metaInfo.Line = line;

            return metaInfo;
        }
    }
}
