using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace RoslynScribe.Domain.Extensions
{
    internal static class IMethodSymbolExtensions
    {
        internal static string GetMethodKey(this IMethodSymbol symbol)
        {
            // Using OriginalDefinition keeps generic arity and signature stable across calls
            return symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        internal static void EnrichMethodContext(this IMethodSymbol symbol, MethodContext methodContext, CSharpSyntaxNode expression, SemanticModel semanticModel, AdcType adcType, AdcMethod adcMethod)
        {
            // enrich only if there are overrides to set
            if (adcMethod != null && adcMethod.SetGuidesOverrides != null && adcMethod.SetGuidesOverrides.Count != 0 ||
                adcType.SetGuidesOverrides != null && adcType.SetGuidesOverrides.Count != 0)
            {
                methodContext.ContainingTypeGenericParameters = symbol.GetGenericTypeParameters();
                methodContext.MethodParametersTypes = symbol.GetParameterTypes();
                var kind = expression.Kind();
                methodContext.ExpressionKind = kind == SyntaxKind.InvocationExpression
                    ? nameof(ExpressionKindsEnum.Invocation)
                    : nameof(ExpressionKindsEnum.Declaration);

                if (kind == SyntaxKind.InvocationExpression)
                {
                    var invocation = expression as InvocationExpressionSyntax;
                    methodContext.MethodArgumentsTypes = invocation.GetArgumentTypes(semanticModel);
                }

                if (adcType.GetAttributes != null)
                {
                    methodContext.ContainingTypeAttributes = symbol.ContainingType.GetAttributes(adcType.GetAttributes);
                }

                if (adcMethod?.GetAttributes != null)
                {
                    methodContext.MethodAttributes = symbol.GetAttributes(adcMethod.GetAttributes);
                }
            }
        }

        internal static MethodContext GetMethodContext(this IMethodSymbol symbol)
        {
            if (symbol is null)
            {
                return null;
            }

            var methodInfo = new MethodContext
            {
                ContainingType = MethodContext.NormalizeTypeFullName(symbol.ContainingType?.ToDisplayString()),
                ContainingTypeName = symbol.ContainingType?.Name,
                MethodName = symbol.Name,
                MethodIdentifier = symbol.GetMethodIdentifier(),
            };

            return methodInfo;
        }

        private static string[] GetParameterTypes(this IMethodSymbol symbol)
        {
            return symbol.Parameters
                .Select(p => MethodContext.NormalizeTypeFullName(p.Type.ToDisplayString()))
                .ToArray();
        }

        private static string GetMethodIdentifier(this IMethodSymbol symbol)
        {
            var key = symbol.GetMethodKey();
            return key.Replace(symbol.OriginalDefinition.ContainingType.ToDisplayString() + ".", string.Empty);
        }

        private static string[] GetGenericTypeParameters(this IMethodSymbol symbol)
        {
            var types = new List<string>();
            if (symbol.ContainingType != null)
            {
                types = symbol.ContainingType.Interfaces
                    .Where(x => !x.IsUnboundGenericType)
                    .SelectMany(x => x.TypeArguments.Select(y => y.ToDisplayString()))
                    .ToList();

                if (symbol.ContainingType.BaseType != null && !symbol.ContainingType.BaseType.IsUnboundGenericType)
                {
                    types.AddRange(symbol.ContainingType.BaseType.TypeArguments.Select(y => y.ToDisplayString()));
                }
            }

            return types.ToArray();
        }
    }
}
