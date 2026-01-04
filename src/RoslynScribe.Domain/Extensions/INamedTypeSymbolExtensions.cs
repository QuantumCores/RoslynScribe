using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Extensions
{
    internal static class INamedTypeSymbolExtensions
    {
        public static IEnumerable<INamedTypeSymbol> GetAllBaseTypesWithGenerics(this INamedTypeSymbol type)
        {
            while (type.BaseType != null)
            {
                yield return type.BaseType;
                type = type.BaseType;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetAllInterfacesWithGenerics(this INamedTypeSymbol type)
        {
            // in case type is a generic interface without subtypes, include the original definition
            if (type.TypeKind == TypeKind.Interface && type.IsGenericType)
            {
                yield return type.OriginalDefinition;
            }

            // otherwise, traverse all subtypes
            foreach (var interfaceType in type.AllInterfaces)
            {
                yield return interfaceType;

                if (interfaceType.IsGenericType)
                {
                    yield return interfaceType.OriginalDefinition;
                }
            }
        }
    }
}
