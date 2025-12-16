using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    public class ScribeResult
    {
        public List<ScribeTreeNode> Trees { get; set; }

        public Dictionary<Guid, ScribeNodeData> Nodes { get; set; }
    }
}
