using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Extensions;
using RoslynScribe.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynScribe.Domain.Services
{
    internal static class ImplementationResolver
    {
        internal static IMethodSymbol ResolveImplementationMethodSymbol(
            IMethodSymbol methodSymbol,
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            AdcConfig adcConfig,
            Trackers trackers)
        {
            if (methodSymbol == null)
            {
                return null;
            }

            var containingType = methodSymbol.ContainingType;
            if (containingType == null)
            {
                return methodSymbol;
            }

            var isInterface = containingType.TypeKind == TypeKind.Interface;
            var isAbstract = methodSymbol.IsAbstract;
            if (!isInterface && !isAbstract)
            {
                return methodSymbol;
            }

            if (TryGetCachedImplementation(methodSymbol, trackers, out var cached))
            {
                return cached;
            }

            if (TryResolveImplementationFromConfig(methodSymbol, adcConfig, trackers, out var fromConfig))
            {
                CacheImplementation(methodSymbol, fromConfig, trackers);
                return fromConfig;
            }

            if (TryResolveImplementationFromAssignment(methodSymbol, invocation, semanticModel, documents, trackers, out var fromAssignment))
            {
                return fromAssignment;
            }

            if (TryResolveUniqueImplementation(methodSymbol, trackers, out var unique))
            {
                CacheImplementation(methodSymbol, unique, trackers);
                return unique;
            }

            return null;
        }

        private static bool TryGetCachedImplementation(IMethodSymbol methodSymbol, Trackers trackers, out IMethodSymbol cached)
        {
            cached = null;
            if (trackers?.ImplementationMethodCache == null)
            {
                return false;
            }

            var key = GetImplementationCacheKey(methodSymbol);
            return trackers.ImplementationMethodCache.TryGetValue(key, out cached);
        }

        private static void CacheImplementation(IMethodSymbol methodSymbol, IMethodSymbol implementation, Trackers trackers)
        {
            if (implementation == null || trackers?.ImplementationMethodCache == null)
            {
                return;
            }

            var key = GetImplementationCacheKey(methodSymbol);
            trackers.ImplementationMethodCache[key] = implementation;
        }

        private static string GetImplementationCacheKey(IMethodSymbol methodSymbol)
        {
            var containingType = methodSymbol.ContainingType?.ToDisplayString();
            return $"{methodSymbol.GetMethodKey()}|{containingType}";
        }

        private static bool TryResolveImplementationFromConfig(
            IMethodSymbol methodSymbol,
            AdcConfig adcConfig,
            Trackers trackers,
            out IMethodSymbol implementation)
        {
            implementation = null;
            if (adcConfig?.InterfaceImplementations == null || adcConfig.InterfaceImplementations.Count == 0)
            {
                return false;
            }

            var interfaceType = methodSymbol.ContainingType;
            if (interfaceType == null)
            {
                return false;
            }

            if (!TryGetImplementationTypeName(adcConfig.InterfaceImplementations, interfaceType, out var implTypeName))
            {
                return false;
            }

            if (!TryResolveTypeSymbolByName(implTypeName, trackers?.Solution, out var implTypeSymbol))
            {
                return false;
            }

            var constructedType = TryConstructImplementationType(implTypeSymbol, interfaceType);
            return TryResolveMethodOnType(methodSymbol, constructedType, out implementation);
        }

        private static bool TryResolveImplementationFromAssignment(
            IMethodSymbol methodSymbol,
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            out IMethodSymbol implementation)
        {
            implementation = null;
            if (invocation == null || semanticModel == null)
            {
                return false;
            }

            var receiverSymbol = GetInvocationReceiverSymbol(invocation, semanticModel);
            if (receiverSymbol == null)
            {
                return false;
            }

            if (!TryGetAssignedExpression(receiverSymbol, invocation, semanticModel, documents, trackers, out var assignedExpression))
            {
                return false;
            }

            if (!TryGetConcreteTypeSymbol(assignedExpression, semanticModel, documents, trackers, out var concreteType))
            {
                return false;
            }

            return TryResolveMethodOnType(methodSymbol, concreteType, out implementation);
        }

        private static bool TryResolveUniqueImplementation(IMethodSymbol methodSymbol, Trackers trackers, out IMethodSymbol implementation)
        {
            implementation = null;
            if (methodSymbol == null || trackers?.Solution == null)
            {
                return false;
            }

            IEnumerable<ISymbol> candidates;
            if (methodSymbol.ContainingType?.TypeKind == TypeKind.Interface)
            {
                candidates = SymbolFinder.FindImplementationsAsync(methodSymbol.OriginalDefinition, trackers.Solution)
                    .GetAwaiter()
                    .GetResult();
            }
            else if (methodSymbol.IsAbstract)
            {
                candidates = SymbolFinder.FindOverridesAsync(methodSymbol.OriginalDefinition, trackers.Solution)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                return false;
            }

            var implementations = candidates
                .OfType<IMethodSymbol>()
                .Where(m => !m.IsAbstract && m.DeclaringSyntaxReferences.Length != 0)
                .ToList();

            if (implementations.Count != 1)
            {
                return false;
            }

            implementation = implementations[0];
            return true;
        }

        private static bool TryResolveMethodOnType(IMethodSymbol methodSymbol, INamedTypeSymbol implementationType, out IMethodSymbol implementation)
        {
            implementation = null;
            if (implementationType == null || methodSymbol == null)
            {
                return false;
            }

            if (methodSymbol.ContainingType?.TypeKind == TypeKind.Interface)
            {
                var impl = implementationType.FindImplementationForInterfaceMember(methodSymbol) as IMethodSymbol;
                if (impl != null && impl.DeclaringSyntaxReferences.Length != 0)
                {
                    implementation = impl;
                    return true;
                }
            }

            if (methodSymbol.IsAbstract)
            {
                var members = implementationType.GetMembers(methodSymbol.Name).OfType<IMethodSymbol>();
                foreach (var member in members)
                {
                    if (member.IsAbstract)
                    {
                        continue;
                    }

                    var overridden = member.OverriddenMethod?.OriginalDefinition;
                    if (overridden != null && SymbolEqualityComparer.Default.Equals(overridden, methodSymbol.OriginalDefinition))
                    {
                        if (member.DeclaringSyntaxReferences.Length != 0)
                        {
                            implementation = member;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool TryGetImplementationTypeName(
            Dictionary<string, string> mappings,
            INamedTypeSymbol interfaceType,
            out string implementationTypeName)
        {
            implementationTypeName = null;
            if (mappings == null || interfaceType == null)
            {
                return false;
            }

            var interfaceDisplay = MethodContext.NormalizeTypeFullName(interfaceType.ToDisplayString());
            var interfaceDefinition = MethodContext.NormalizeTypeFullName(interfaceType.OriginalDefinition.ToDisplayString());

            foreach (var mapping in mappings)
            {
                var key = MethodContext.NormalizeTypeFullName(mapping.Key);
                if (string.Equals(key, interfaceDisplay, StringComparison.Ordinal) ||
                    string.Equals(key, interfaceDefinition, StringComparison.Ordinal))
                {
                    implementationTypeName = mapping.Value;
                    return !string.IsNullOrWhiteSpace(implementationTypeName);
                }
            }

            return false;
        }

        private static bool TryResolveTypeSymbolByName(string typeName, Solution solution, out INamedTypeSymbol typeSymbol)
        {
            typeSymbol = null;
            if (string.IsNullOrWhiteSpace(typeName) || solution == null)
            {
                return false;
            }

            var normalized = MethodContext.NormalizeTypeFullName(typeName);
            var lookupName = ExtractTypeNameForLookup(normalized, out var namespaceName, out var arity);
            var candidates = solution.Projects
                .SelectMany(project => SymbolFinder.FindDeclarationsAsync(project, lookupName, ignoreCase: false)
                    .GetAwaiter()
                    .GetResult())
                .OfType<INamedTypeSymbol>();

            foreach (var candidate in candidates)
            {
                var candidateDisplay = MethodContext.NormalizeTypeFullName(candidate.ToDisplayString());
                var candidateDefinition = MethodContext.NormalizeTypeFullName(candidate.OriginalDefinition.ToDisplayString());
                if (string.Equals(candidateDisplay, normalized, StringComparison.Ordinal) ||
                    string.Equals(candidateDefinition, normalized, StringComparison.Ordinal))
                {
                    typeSymbol = candidate;
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(namespaceName) &&
                    string.Equals(candidate.ContainingNamespace.ToDisplayString(), namespaceName, StringComparison.Ordinal) &&
                    string.Equals(candidate.Name, lookupName, StringComparison.Ordinal) &&
                    (!arity.HasValue || candidate.Arity == arity.Value))
                {
                    typeSymbol = candidate;
                    return true;
                }
            }

            return false;
        }

        private static string ExtractTypeNameForLookup(string typeName, out string namespaceName, out int? arity)
        {
            namespaceName = null;
            arity = null;
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return typeName;
            }

            var trimmed = typeName.Trim();
            var genericStart = trimmed.IndexOf('<');
            if (genericStart >= 0)
            {
                var genericPart = trimmed.Substring(genericStart + 1, trimmed.Length - genericStart - 2);
                if (genericPart.Length == 0)
                {
                    arity = 1;
                }
                else
                {
                    arity = genericPart.Split(',').Length;
                }

                trimmed = trimmed.Substring(0, genericStart);
            }

            var lastDot = trimmed.LastIndexOf('.');
            var lastPlus = trimmed.LastIndexOf('+');
            var separatorIndex = Math.Max(lastDot, lastPlus);
            if (separatorIndex >= 0)
            {
                namespaceName = trimmed.Substring(0, separatorIndex);
                return trimmed.Substring(separatorIndex + 1);
            }

            return trimmed;
        }

        private static INamedTypeSymbol TryConstructImplementationType(INamedTypeSymbol implementationType, INamedTypeSymbol interfaceType)
        {
            if (implementationType == null || interfaceType == null)
            {
                return implementationType;
            }

            if (!interfaceType.IsGenericType)
            {
                return implementationType;
            }

            var typeArguments = interfaceType.TypeArguments;
            if (typeArguments.Length == 0)
            {
                return implementationType;
            }

            if (implementationType.IsGenericType && implementationType.Arity == typeArguments.Length)
            {
                var definition = implementationType.IsUnboundGenericType
                    ? implementationType.OriginalDefinition
                    : implementationType;
                if (definition.IsGenericType)
                {
                    return definition.Construct(typeArguments.ToArray());
                }
            }

            return implementationType;
        }

        private static ISymbol GetInvocationReceiverSymbol(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
        {
            if (invocation == null || semanticModel == null)
            {
                return null;
            }

            ExpressionSyntax receiver = null;
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                receiver = memberAccess.Expression;
            }
            else if (invocation.Expression is MemberBindingExpressionSyntax)
            {
                var conditional = invocation.Parent as ConditionalAccessExpressionSyntax;
                receiver = conditional?.Expression;
            }

            return receiver == null ? null : semanticModel.GetSymbolInfo(receiver).Symbol;
        }

        private static bool TryGetAssignedExpression(
            ISymbol receiverSymbol,
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            out ExpressionSyntax assignedExpression)
        {
            assignedExpression = null;
            if (receiverSymbol == null)
            {
                return false;
            }

            var candidates = new List<ExpressionSyntax>();
            switch (receiverSymbol)
            {
                case ILocalSymbol local:
                    AddLocalAssignmentCandidates(local, invocation, semanticModel, documents, trackers, candidates);
                    break;
                case IFieldSymbol field:
                    AddFieldAssignmentCandidates(field, semanticModel, documents, trackers, candidates);
                    break;
                case IParameterSymbol parameter:
                    AddParameterAssignmentCandidates(parameter, invocation, semanticModel, documents, trackers, candidates);
                    break;
                default:
                    return false;
            }

            if (candidates.Count != 1)
            {
                return false;
            }

            assignedExpression = candidates[0];
            return true;
        }

        private static void AddLocalAssignmentCandidates(
            ILocalSymbol local,
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            List<ExpressionSyntax> candidates)
        {
            var declarator = local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
            if (declarator?.Initializer?.Value != null)
            {
                candidates.Add(declarator.Initializer.Value);
            }

            var scope = GetContainingExecutableScope(invocation);
            AddAssignmentCandidates(scope, local, semanticModel, documents, trackers, invocation, candidates);
        }

        private static void AddFieldAssignmentCandidates(
            IFieldSymbol field,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            List<ExpressionSyntax> candidates)
        {
            foreach (var reference in field.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is VariableDeclaratorSyntax declarator && declarator.Initializer?.Value != null)
                {
                    candidates.Add(declarator.Initializer.Value);
                }
            }

            var fieldDeclaration = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            var typeDeclaration = fieldDeclaration?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeDeclaration == null)
            {
                return;
            }

            foreach (var constructor in typeDeclaration.Members.OfType<ConstructorDeclarationSyntax>())
            {
                AddAssignmentCandidates(constructor, field, semanticModel, documents, trackers, null, candidates);
            }
        }

        private static void AddParameterAssignmentCandidates(
            IParameterSymbol parameter,
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            List<ExpressionSyntax> candidates)
        {
            var scope = GetContainingExecutableScope(invocation);
            AddAssignmentCandidates(scope, parameter, semanticModel, documents, trackers, invocation, candidates);
        }

        private static void AddAssignmentCandidates(
            SyntaxNode scope,
            ISymbol symbol,
            SemanticModel currentSemanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            InvocationExpressionSyntax invocation,
            List<ExpressionSyntax> candidates)
        {
            if (scope == null)
            {
                return;
            }

            foreach (var assignment in scope.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (!assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    continue;
                }

                if (invocation != null && assignment.SyntaxTree == invocation.SyntaxTree && assignment.SpanStart > invocation.SpanStart)
                {
                    continue;
                }

                var model = GetSemanticModelForNode(assignment, currentSemanticModel, documents, trackers);
                var leftSymbol = model.GetSymbolInfo(assignment.Left).Symbol;
                if (SymbolEqualityComparer.Default.Equals(leftSymbol, symbol))
                {
                    candidates.Add(assignment.Right);
                }
            }
        }

        private static SyntaxNode GetContainingExecutableScope(SyntaxNode node)
        {
            if (node == null)
            {
                return null;
            }

            var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method != null)
            {
                return method.Body ?? (SyntaxNode)method.ExpressionBody?.Expression;
            }

            var constructor = node.FirstAncestorOrSelf<ConstructorDeclarationSyntax>();
            if (constructor != null)
            {
                return constructor.Body ?? (SyntaxNode)constructor.ExpressionBody?.Expression;
            }

            var accessor = node.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
            if (accessor != null)
            {
                return accessor.Body ?? (SyntaxNode)accessor.ExpressionBody?.Expression;
            }

            var localFunction = node.FirstAncestorOrSelf<LocalFunctionStatementSyntax>();
            if (localFunction != null)
            {
                return localFunction.Body ?? (SyntaxNode)localFunction.ExpressionBody?.Expression;
            }

            return null;
        }

        private static bool TryGetConcreteTypeSymbol(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers,
            out INamedTypeSymbol concreteType)
        {
            concreteType = null;
            if (expression == null || !IsSupportedAssignmentExpression(expression))
            {
                return false;
            }

            var model = GetSemanticModelForNode(expression, semanticModel, documents, trackers);
            var typeInfo = model.GetTypeInfo(expression);
            var typeSymbol = typeInfo.Type ?? typeInfo.ConvertedType;
            if (typeSymbol is INamedTypeSymbol namedType)
            {
                if ((namedType.TypeKind == TypeKind.Class && !namedType.IsAbstract) ||
                    namedType.TypeKind == TypeKind.Struct)
                {
                    concreteType = namedType;
                    return true;
                }
            }

            return false;
        }

        private static bool IsSupportedAssignmentExpression(ExpressionSyntax expression)
        {
            return expression is ObjectCreationExpressionSyntax || expression is InvocationExpressionSyntax;
        }

        private static SemanticModel GetSemanticModelForNode(
            SyntaxNode node,
            SemanticModel currentSemanticModel,
            Dictionary<string, Document> documents,
            Trackers trackers)
        {
            if (node == null)
            {
                return currentSemanticModel;
            }

            var path = node.SyntaxTree?.FilePath;
            if (string.IsNullOrWhiteSpace(path) || documents == null || trackers == null || !documents.ContainsKey(path))
            {
                return currentSemanticModel;
            }

            return GetSemanticModel(path, currentSemanticModel, documents, trackers.SemanticModelCache);
        }

        private static SemanticModel GetSemanticModel(
            string documentPath,
            SemanticModel currentSemanticModel,
            Dictionary<string, Document> documents,
            Dictionary<string, SemanticModel> cache)
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
    }
}
