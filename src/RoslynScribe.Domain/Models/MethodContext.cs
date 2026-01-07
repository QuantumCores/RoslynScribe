using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
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
