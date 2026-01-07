using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynScribe.Domain.Extensions
{
    internal static class ISymbolExtensions
    {
        internal static Dictionary<string, string> GetAttributes(this ISymbol symbol, HashSet<string> requiredAttributes)
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
}
