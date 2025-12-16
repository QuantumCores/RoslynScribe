using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    public class ScribeTreeNode : IScribeNode
    {
        public Guid Id { get; set; }

        public List<ScribeTreeNode> ChildNodes { get; set; } = new List<ScribeTreeNode>();
    }
}

