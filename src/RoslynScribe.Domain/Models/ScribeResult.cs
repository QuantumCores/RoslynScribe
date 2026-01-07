using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    public class ScribeResult
    {
        public List<ScribeTreeNode> Trees { get; set; }

        public Dictionary<string, ScribeNodeData> Nodes { get; set; }
    }
}
