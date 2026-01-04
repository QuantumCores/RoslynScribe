using Microsoft.CodeAnalysis;
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

        internal static MethodInfo GetMethodInfo(this IMethodSymbol symbol)
        {
            if (symbol is null)
            {
                return null;
            }

            var methodInfo = new MethodInfo
            {
                ContainingType = MethodInfo.NormalizeTypeFullName(symbol.ContainingType?.ToDisplayString()),
                ContainingTypeGenericParameters = GetGenericTypeParameters(symbol),
                MethodName = symbol.Name,
                MethodIdentifier = symbol.GetMethodIdentifier(),
                ParametersTypes = GetParameterTypes(symbol)
            };

            return methodInfo;
        }

        private static string[] GetParameterTypes(IMethodSymbol symbol)
        {
            return symbol.Parameters
                                .Select(p => MethodInfo.NormalizeTypeFullName(p.Type.ToDisplayString()))
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

    internal class MethodInfo
    {
        internal string ContainingType { get; set; }

        internal string[] ContainingTypeGenericParameters { get; set; }

        internal string[] ParametersTypes { get; set; }

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
