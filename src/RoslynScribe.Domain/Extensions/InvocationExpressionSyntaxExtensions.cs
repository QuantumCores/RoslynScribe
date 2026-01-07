using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynScribe.Domain.Models;
using System;

namespace RoslynScribe.Domain.Extensions
{
    internal static class InvocationExpressionSyntaxExtensions
    {
        internal static IMethodSymbol GetMethodSymbol(this InvocationExpressionSyntax syntaxNode, SemanticModel semanticModel)
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(syntaxNode);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            return methodSymbol;
        }

        internal static string[] GetArgumentTypes(this InvocationExpressionSyntax syntaxNode, SemanticModel semanticModel)
        {
            if (syntaxNode?.ArgumentList == null || semanticModel == null)
            {
                return Array.Empty<string>();
            }

            var arguments = syntaxNode.ArgumentList.Arguments;
            if (arguments.Count == 0)
            {
                return Array.Empty<string>();
            }

            var result = new string[arguments.Count];
            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                var typeInfo = semanticModel.GetTypeInfo(argument.Expression);
                var typeSymbol = typeInfo.Type ?? typeInfo.ConvertedType;
                result[i] = MethodContext.NormalizeTypeFullName(typeSymbol?.ToDisplayString());
            }

            return result;
        }
    }
}
