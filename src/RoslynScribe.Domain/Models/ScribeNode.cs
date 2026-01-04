using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    public class ScribeNode : IScribeNode
    {
        public Guid Id { get; set; }

        // This is id of node which this node represents
        // public Guid? TargetNodeId { get; set; }

        // commens in their original form
        public string[] Value { get; set; }

        public ScribeGuides Guides { get; set; }

        public string Kind { get; set; }

        public MetaInfo MetaInfo { get; set; }

        public List<ScribeNode> ChildNodes { get; set; } = new List<ScribeNode>();

        public override string ToString()
        {
            return Kind + " - " + (Value == null ? "NV" : string.Join(" | ", Value));
        }
    }
}
