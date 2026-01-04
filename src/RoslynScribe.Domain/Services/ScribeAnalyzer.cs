using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using RoslynScribe.Domain.Configuration;
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
        // public const string GuidesLabel = "[ADG]";

        private static readonly string[] Starts = { $"//{CommentLabel}", $"// {CommentLabel}" };

        public static async Task<List<ScribeNode>> Analyze(MSBuildWorkspace workspace, Solution solution, AdcConfig adcConfig)
        {
            var documents = solution.Projects.SelectMany(x => x.Documents).ToDictionary(x => x.FilePath);
            var result = new List<ScribeNode>();
            var trackers = new Trackers();

            foreach (var project in solution.Projects)
            {
                if (project.Name == "RoslynScribe.TestProject")
                {
                    foreach (var document in project.Documents)
                    {
                        var node = await Analyze(project, documents, document, trackers, adcConfig);
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

        internal static Task<ScribeNode> Analyze(Solution solution, string projectName, string documentName, AdcConfig adcConfig)
        {
            var project = solution.Projects.Single(x => x.Name == projectName);
            var documents = solution.Projects.SelectMany(x => x.Documents).ToDictionary(x => x.FilePath);
            var document = project.Documents.Single(x => x.Name == documentName);
            return Analyze(project, documents, document, new Trackers(), adcConfig);
        }

        private static async Task<ScribeNode> Analyze(
            Project project,
            Dictionary<string, Document> documents,
            Document document,
            Trackers trackers,
            AdcConfig adcConfig)
        {
            if (document.Name.EndsWith(".AssemblyInfo.cs")
                || document.Name.EndsWith(".GlobalUsings.g.cs")
                || document.Name.EndsWith("AssemblyAttributes.cs"))
            {
                return null;
            }

            // Console.WriteLine($"{document.Name}");
            var semanticModel = await document.GetSemanticModelAsync();
            var tree = await document.GetSyntaxTreeAsync();
            var rootNode = await tree.GetRootAsync();

            var documentMeta = new MetaInfo
            {
                ProjectName = project.Name,
                DocumentName = document.Name,
                DocumentPath = document.FilePath,
                Identifier = document.FilePath
            };

            var scribeNode = new ScribeNode()
            {
                Id = documentMeta.GetDeterministicId(),
                Kind = "Document",
                MetaInfo = documentMeta
            };

            trackers.SemanticModelCache.Add(document.FilePath, semanticModel);

            Traverse(rootNode, scribeNode, semanticModel, documents, trackers, adcConfig);

            // SyntaxTreePrinter.Print(rootNode);            
            // ScribeTreePrinter.Print(scribeNode);
            // Console.WriteLine();
            // Console.WriteLine();

            return scribeNode;
        }

        private static void Traverse(
            SyntaxNode node,
            ScribeNode parentNode,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            AdcConfig adcConfig)
        {
            var nodes = node.ChildNodes();
            foreach (var syntaxNode in nodes)
            {
                var kind = syntaxNode.Kind();
                if (KindToSkip(kind))
                {
                    continue;
                }

                ProcessNode(syntaxNode, kind, parentNode, semanticModel, documents, trackers, adcConfig);
            }
        }

        private static void ProcessNode(
            SyntaxNode syntaxNode,
            SyntaxKind syntaxKind,
            ScribeNode parentNode,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            AdcConfig adcConfig)
        {
            SetMetaInfo(syntaxNode, syntaxKind, parentNode);
            var lTrivias = syntaxNode.GetLeadingTrivia();

            ScribeNode childNode = null;
            if (lTrivias.Count != 0)
            {
                childNode = ProcessCommentTrivia(syntaxNode, syntaxKind, parentNode, lTrivias, semanticModel, trackers);

                if (childNode != null)
                {
                    // if trackers contains node with that id it means that this path was already processed
                    if (trackers.Nodes.ContainsKey(childNode.Id))
                    {
                        return;
                    }

                    trackers.Nodes.Add(childNode.Id, childNode);
                }
            }

            var currentParent = childNode ?? parentNode;

            if (syntaxKind == SyntaxKind.InvocationExpression)
            {
                var invocation = syntaxNode as InvocationExpressionSyntax;
                ProcessInvocation(invocation, currentParent, semanticModel, documents, trackers, adcConfig);
            }

            if (syntaxKind == SyntaxKind.MethodDeclaration)
            {
                var declaration = syntaxNode as MethodDeclarationSyntax;
                ProcessDeclaration(declaration, parentNode, semanticModel, documents, trackers, adcConfig);
            }

            Traverse(syntaxNode, currentParent, semanticModel, documents, trackers, adcConfig);
        }

        private static void ProcessInvocation(
            InvocationExpressionSyntax invocation,
            ScribeNode parentNode,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            AdcConfig adcConfig)
        {
            var invokedMethod = invocation.GetMethodSymbol(semanticModel);
            if (invokedMethod == null)
            {
                return;
            }

            var configuredNode = GetNodeFromConfiguration(invocation, parentNode, invokedMethod, adcConfig, semanticModel, trackers);
            var currentParent = configuredNode ?? parentNode;

            // Avoid infinite recursion
            var methodKey = invokedMethod.GetMethodKey();
            if (!trackers.RecursionStack.Add(methodKey))
            {
                return;
            }

            // Expand into the invoked method so comments inside it become part of the tree under the configured node.
            var syntaxReferences = invokedMethod.DeclaringSyntaxReferences;
            for (int i = 0; i < syntaxReferences.Length; i++)
            {
                var location = invokedMethod.Locations[i].GetLineSpan().Path;
                var contextSemanticModel = GetSemanticModel(location, semanticModel, documents, trackers.SemanticModelCache);
                var methodNode = syntaxReferences[i].GetSyntax();
                ProcessNode(methodNode, methodNode.Kind(), currentParent, contextSemanticModel, documents, trackers, adcConfig);
            }

            trackers.RecursionStack.Remove(methodKey);
            return;
        }

        private static void ProcessDeclaration(
            MethodDeclarationSyntax declaration,
            ScribeNode parentNode,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            AdcConfig adcConfig)
        {
            var declaredMethod = semanticModel.GetDeclaredSymbol(declaration);
            if (declaredMethod == null)
            {
                return;
            }

            var configuredNode = GetNodeFromConfiguration(declaration, parentNode, declaredMethod, adcConfig, semanticModel, trackers);
        }

        /// <summary>
        /// Attempts to create a configured ScribeNode for the specified method symbol based on the provided
        /// configuration and method symbol.
        /// </summary>
        /// <remarks>
        /// The method first attempts to find a configuration match for the method on its
        /// containing type. If no match is found, it searches implemented interfaces for a suitable configuration.
        /// Returns null if no configuration applies.
        /// </remarks>
        /// <param name="expression">The syntax node representing the method invocation expression to analyze.</param>
        /// <param name="parentNode">The parent ScribeNode to which the new node will be attached, or null if this is the root node.</param>
        /// <param name="methodSymbol">The symbol representing the method being invoked. Used to match against configuration.</param>
        /// <param name="adcConfig">The configuration object containing type and method mapping information. Must not be null and must contain
        /// at least one configured type.</param>
        /// <param name="trackers">The Trackers instance used to manage state or context during node creation.</param>
        /// <returns>
        /// A configured ScribeNode representing the method invocation if a matching configuration is found; otherwise, null.
        /// </returns>
        private static ScribeNode GetNodeFromConfiguration(
            CSharpSyntaxNode expression,
            ScribeNode parentNode,
            IMethodSymbol methodSymbol,
            AdcConfig adcConfig,
            SemanticModel semanticModel,
            Trackers trackers)
        {
            if (adcConfig?.Types == null || adcConfig.Types.Count == 0)
            {
                return null;
            }

            // var candidates = GetCandidateMethods(invokedMethod);

            AdcType adcType = null;
            AdcMethod adcMethod = null;
            var expressionKind = expression.Kind();
            var originalInfo = methodSymbol.GetMethodInfo();
            if (TryFindConfiguredType(adcConfig, originalInfo, out adcType))
            {
                if (TryFindConfiguredMethod(adcType, originalInfo, out adcMethod))
                {
                    if (expressionKind == SyntaxKind.MethodDeclaration && adcMethod != null && !adcMethod.IncludeMethodDeclaration)
                    {
                        return null;
                    }
                    return AddConfiguredNode(expression, expressionKind, parentNode, semanticModel, trackers, adcType, adcMethod, originalInfo);
                }
            }

            // if method is not found directly on containing type, try interfaces
            MethodInfo info = null;
            var containingType = methodSymbol.ContainingType;
            if (containingType != null)
            {
                foreach (var iface in containingType.GetAllInterfacesWithGenerics())
                {
                    // this might not work with nested namespaces
                    var fullName = iface.ToDisplayString();
                    if (!adcConfig.Types.ContainsKey(fullName))
                    {
                        continue;
                    }

                    foreach (var member in iface.GetMembers(methodSymbol.Name))
                    {
                        if (member is IMethodSymbol ifaceMethod)
                        {
                            //var impl = containingType.FindImplementationForInterfaceMember(ifaceMethod) as IMethodSymbol;
                            //if (SymbolEqualityComparer.Default.Equals(impl, invokedMethod))
                            {
                                info = ifaceMethod.GetMethodInfo();
                                if (TryFindConfiguredType(adcConfig, info, out adcType))
                                {
                                    if (TryFindConfiguredMethod(adcType, info, out adcMethod))
                                    {
                                        if (expressionKind == SyntaxKind.MethodDeclaration && adcMethod != null && !adcMethod.IncludeMethodDeclaration)
                                        {
                                            return null;
                                        }
                                        return AddConfiguredNode(expression, expressionKind, parentNode, semanticModel, trackers, adcType, adcMethod, originalInfo);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private static bool TryFindConfiguredType(AdcConfig adcConfig, MethodInfo candidate, out AdcType adcType)
        {
            adcType = null;

            if (candidate is null)
            {
                return false;
            }

            if (!adcConfig.Types.ContainsKey(candidate.ContainingType))
            {
                return false;
            }

            adcType = adcConfig.Types[candidate.ContainingType];
            return true;
        }

        private static bool TryFindConfiguredMethod(AdcType adcType, MethodInfo candidate, out AdcMethod adcMethod)
        {
            adcMethod = null;
            if (adcType.Methods.Length == 0)
            {
                return true;
            }

            foreach (var method in adcType.Methods)
            {
                if (method == null ||
                    (string.IsNullOrWhiteSpace(method.MethodName) && string.IsNullOrWhiteSpace(method.MethodIdentifier)))
                {
                    continue;
                }

                // Prefer matching by MethodIdentifier if provided; fallback to MethodName.
                if (!string.IsNullOrWhiteSpace(method.MethodIdentifier))
                {
                    if (string.Equals(method.MethodIdentifier, candidate.MethodIdentifier, StringComparison.Ordinal))
                    {
                        adcMethod = method;
                        return true;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(method.MethodName))
                {
                    if (string.Equals(method.MethodName, candidate.MethodName, StringComparison.Ordinal))
                    {
                        adcMethod = method;
                        return true;
                    }
                }
            }

            return false;
        }

        private static ScribeNode ProcessCommentTrivia(SyntaxNode syntaxNode, SyntaxKind syntaxKind, ScribeNode parentNode, SyntaxTriviaList syntaxTrivias, SemanticModel semanticModel, Trackers trackers)
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
                var array = comments.ToArray();
                var guides = ScribeCommnetParser.Parse(array);
                return AddChildNode(syntaxNode, syntaxKind, parentNode, array, guides, semanticModel, line, trackers);
            }

            return null;
        }

        private static ScribeNode AddConfiguredNode(CSharpSyntaxNode expression, SyntaxKind syntaxKind, ScribeNode parentNode, SemanticModel semanticModel, Trackers trackers, AdcType adcType, AdcMethod adcMethod, MethodInfo info)
        {
            var line = expression.GetLocation().GetLineSpan().Span.Start.Line;
            var level = adcMethod != null ? adcMethod.Level : 1;

            var guideText = $"{info.ContainingType}.{info.MethodIdentifier}";
            var value = new string[] {
                $"// {CommentLabel}[{ScribeGuidesTokens.Text}:`{guideText}`,{ScribeGuidesTokens.Level}:`{level}`]",
                // $"// {CommentLabel}[{ScribeGuidesTokens.Tags}:`{info.TypeFullName}.{info.MethodIdentifier}`]"
            };
            var guides = new ScribeGuides
            {
                Level = level,
                Text = guideText,
            };
            guides = GuidesOverridesParser.Apply(adcMethod.GuidesOverrides, guides, info);

            var configuredNode = AddChildNode(expression, syntaxKind, parentNode, value, guides, semanticModel, line, trackers);
            return configuredNode;
        }

        /// <summary>
        /// Adds a child node to the specified parent node if it does not already exist, and returns the added or
        /// existing child node.
        /// </summary>
        /// <remarks>
        /// If a child node with the same identifier already exists under the parent node, or if
        /// the parent node has the same identifier, the method returns null and does not add a new node. The method
        /// does not update the trackers object.
        /// </remarks>
        /// <param name="syntaxNode">The syntax node from which to create the child node.</param>
        /// <param name="syntaxKind">The kind of syntax represented by the child node.</param>
        /// <param name="parentNode">The parent node to which the child node will be added.</param>
        /// <param name="value">An array of string values associated with the child node. May be null.</param>
        /// <param name="semanticModel">The semantic model used to provide additional context for the syntax node.</param>
        /// <param name="line">The line number in the source code associated with the child node.</param>
        /// <param name="trackers">The trackers object used to manage and look up existing nodes.</param>
        /// <returns>
        /// The added or existing child node if it was successfully added; otherwise, null if a node with the same
        /// identifier already exists as a child or is the parent node itself.
        /// </returns>
        private static ScribeNode AddChildNode(SyntaxNode syntaxNode, SyntaxKind syntaxKind, ScribeNode parentNode, string[] value, ScribeGuides guides, SemanticModel semanticModel, int line, Trackers trackers)
        {
            var metaInfo = GetMetaInfo(syntaxNode, syntaxKind, parentNode, semanticModel, line);
            var id = metaInfo.GetDeterministicId();

            if (parentNode.ChildNodes.Any(x => x.Id == id) || parentNode.Id == id)
            {
                return null;
            }

            // do not add to trackers here!
            ScribeNode childNode = null;
            if (!trackers.Nodes.ContainsKey(id))
            {
                childNode = new ScribeNode
                {
                    Id = id,
                    MetaInfo = metaInfo,
                    Kind = syntaxKind.ToString(),
                    Value = value,
                    Guides = guides,                    
                };
            }
            else
            {
                childNode = trackers.Nodes[id];
            }

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

        private static SemanticModel GetSemanticModel(string documentPath, SemanticModel currentSemanticModel, Dictionary<string, Document> documents, Dictionary<string, SemanticModel> cache)
        {
            if (cache.TryGetValue(documentPath, out var semanticModel))
            {
                return semanticModel;
            }

            if (string.Equals(documentPath, currentSemanticModel.SyntaxTree.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                cache[documentPath] = currentSemanticModel;
                return currentSemanticModel;
            }

            var model = documents[documentPath].GetSemanticModelAsync().Result;
            cache[documentPath] = model;
            return model;
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

        private static bool KindToSkip(SyntaxKind kind)
        {
            return kind == SyntaxKind.Attribute ||
                kind == SyntaxKind.AttributeArgument ||
                kind == SyntaxKind.AttributeArgumentList ||
                kind == SyntaxKind.AttributeList ||
                kind == SyntaxKind.AttributeTargetSpecifier ||
                kind == SyntaxKind.CompilationUnit ||
                kind == SyntaxKind.PredefinedType ||
                kind == SyntaxKind.ParameterList; // TODO verify if method invocation inside parameter list is processed
        }
    }
}
