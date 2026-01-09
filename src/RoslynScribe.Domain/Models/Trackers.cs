using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    internal class Trackers
    {
        internal Solution Solution { get; set; }
        internal string SolutionDirectory { get; set; }
        internal Dictionary<string, SemanticModel> SemanticModelCache = new Dictionary<string, SemanticModel>(StringComparer.OrdinalIgnoreCase);        
        internal HashSet<string> RecursionStack = new HashSet<string>(StringComparer.Ordinal);
        internal Dictionary<string, ScribeNode> Nodes = new Dictionary<string, ScribeNode>();
        internal Dictionary<string, IMethodSymbol> ImplementationMethodCache = new Dictionary<string, IMethodSymbol>(StringComparer.Ordinal);
    }
}
