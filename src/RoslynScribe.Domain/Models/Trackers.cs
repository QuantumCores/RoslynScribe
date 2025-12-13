using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    internal class Trackers
    {
        internal Dictionary<string, SemanticModel> SemanticModelCache = new Dictionary<string, SemanticModel>(StringComparer.OrdinalIgnoreCase);        
        internal HashSet<string> RecursionStack = new HashSet<string>(StringComparer.Ordinal);
        internal Dictionary<Guid, ScribeNode> Nodes = new Dictionary<Guid, ScribeNode>();
    }
}
