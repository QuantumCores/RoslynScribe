using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
    }
}
