using Microsoft.CodeAnalysis;
using System;

namespace RoslynScribe.Domain.Extensions
{
    internal static class IMethodSymbolExtensions
    {
        internal static string GetMethodKey(this IMethodSymbol symbol)
        {
            // Using OriginalDefinition keeps generic arity and signature stable across calls
            return symbol.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        internal static MethodInfo GetMethodInfo(this IMethodSymbol symbol)
        {
            if (symbol is null)
            {
                return null;
            }           

            var methodInfo = new MethodInfo
            {
                TypeFullName = MethodInfo.NormalizeTypeFullName(symbol.ContainingType?.ToDisplayString()),
                MethodName = symbol.Name,
                MethodIdentifier = symbol.GetMethodIdentifier()
            };

            return methodInfo;
        }

        private static string GetMethodIdentifier(this IMethodSymbol symbol)
        {
            var key = symbol.GetMethodKey();
            var index = key.LastIndexOf('.');
            return index >= 0 && index < key.Length - 1 ? key.Substring(index + 1) : key;
        }
    }



    internal class MethodInfo
    {
        internal string TypeFullName { get; set; }

        internal string MethodName { get; set; }

        internal string MethodIdentifier { get; set; }

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
