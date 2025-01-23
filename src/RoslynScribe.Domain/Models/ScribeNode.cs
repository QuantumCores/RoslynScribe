using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    public class ScribeNode
    {
        public ScribeNode ParentNode { get; set; }

        public List<ScribeNode> ChildNodes { get; set; } = new List<ScribeNode>();

        public MetaInfo MetaInfo { get; set; }

        public string Kind { get; set; }

        public string[] Value { get; set; }

        public override string ToString()
        {
            return Kind + " - " + (Value == null ? "NV" : string.Join(" | ", Value));
        }
    }
}
