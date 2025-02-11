using System.Text.Json.Serialization;

namespace RoslynScribe.Domain.Models
{
    public class ScribeRefNode : IScribeNode
    {
        public int Id { get; set; }

        [JsonIgnore]
        public ScribeNode ParentNode { get; set; }

        // This id node Id which this node represents
        public int TargetNodeId {  get; set; }
    }
}
