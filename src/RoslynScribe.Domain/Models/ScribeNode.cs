using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RoslynScribe.Domain.Models
{
    public class ScribeNode
    {
        [JsonIgnore]
        public ScribeNode ParentNode { get; set; }

        public string[] Value { get; set; }

        public string Kind { get; set; }

        public MetaInfo MetaInfo { get; set; }

        public List<ScribeNode> ChildNodes { get; set; } = new List<ScribeNode>();

        public override string ToString()
        {
            return Kind + " - " + (Value == null ? "NV" : string.Join(" | ", Value));
        }
    }
}
