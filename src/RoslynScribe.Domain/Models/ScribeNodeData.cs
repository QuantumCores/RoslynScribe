using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    public class ScribeNodeData : IScribeNode
    {
        public Guid Id { get; set; }

        // comments in their original form
        public string[] Value { get; set; }

        public ScribeComment Comment { get; set; }

        public string Kind { get; set; }

        public MetaInfo MetaInfo { get; set; }

        public List<Guid> ChildNodeIds { get; set; } = new List<Guid>();
    }
}

