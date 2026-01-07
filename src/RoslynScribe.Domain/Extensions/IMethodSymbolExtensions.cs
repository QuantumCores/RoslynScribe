using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynScribe.Domain.Configuration;
using System;
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
                methodContext.ContainingTypeGenericParameters = GetGenericTypeParameters(symbol);
                methodContext.MethodParametersTypes = GetParameterTypes(symbol);
                var kind = expression.Kind();
                methodContext.ExpressionKind = kind.ToString().Replace("Expression", "");

                if (kind == SyntaxKind.InvocationExpression)
                {
                    var invocation = expression as InvocationExpressionSyntax;
                    methodContext.MethodArgumentsTypes = invocation.GetArgumentTypes(semanticModel);
                }

                if (adcType.GetAttributes != null)
                {
                    methodContext.ContainingTypeAttributes = GetAttributes(symbol.ContainingType, adcType.GetAttributes);
                }

                if (adcMethod?.GetAttributes != null)
                {
                    methodContext.MethodAttributes = GetAttributes(symbol, adcMethod.GetAttributes);
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

        private static string[] GetParameterTypes(IMethodSymbol symbol)
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

        private static Dictionary<string, string> GetAttributes(ISymbol symbol, HashSet<string> requiredAttributes)
        {
            var requiredIsEmpty = requiredAttributes.Count == 0;
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var attribute in symbol.GetAttributes())
            {
                var name = NormalizeAttributeName(attribute.AttributeClass?.Name);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!result.ContainsKey(name) && (requiredIsEmpty || requiredAttributes.Contains(name)))
                {
                    result.Add(name, GetAttributeValue(attribute));
                }
            }

            return result;
        }

        private static string NormalizeAttributeName(string name)
        {
            const string suffix = "Attribute";
            if (name.EndsWith(suffix, StringComparison.Ordinal))
            {
                return name.Substring(0, name.Length - suffix.Length);
            }

            return name;
        }

        private static string GetAttributeValue(AttributeData attribute)
        {
            var parts = new List<string>();

            foreach (var argument in attribute.ConstructorArguments)
            {
                parts.Add(FormatTypedConstant(argument));
            }

            foreach (var argument in attribute.NamedArguments)
            {
                parts.Add($"{argument.Key}={FormatTypedConstant(argument.Value)}");
            }

            return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
        }

        private static string FormatTypedConstant(TypedConstant constant)
        {
            if (constant.IsNull)
            {
                return "null";
            }

            if (constant.Kind == TypedConstantKind.Array)
            {
                return string.Join(", ", constant.Values.Select(FormatTypedConstant));
            }

            if (constant.Value is ITypeSymbol typeSymbol)
            {
                return typeSymbol.ToDisplayString();
            }

            return constant.Value?.ToString() ?? string.Empty;
        }
    }

    internal class MethodContext
    {
        internal string ContainingType { get; set; }

        internal string ContainingTypeName { get; set; }

        internal Dictionary<string, string> ContainingTypeAttributes { get; set; }

        internal string[] ContainingTypeGenericParameters { get; set; }

        internal string[] MethodParametersTypes { get; set; }

        internal string[] MethodArgumentsTypes { get; set; }

        internal string MethodName { get; set; }

        internal string MethodIdentifier { get; set; }
        public string ExpressionKind { get; internal set; }

        internal Dictionary<string, string> MethodAttributes { get; set; }

        internal static string NormalizeTypeFullName(string typeFullName)
        {
            if (string.IsNullOrWhiteSpace(typeFullName))
            {
                return typeFullName;
            }

            const string prefix = "global::";
            if (typeFullName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return typeFullName.Substring(prefix.Length);
            }

            return typeFullName;
        }
    }
}
