using System.Collections.Generic;

namespace RoslynScribe
{
    public class ScribeNode
    {
        public List<ScribeNode> ChildNodes { get; set; } = new List<ScribeNode>();

        public string NameSpace { get; set; }

        public string Kind { get; set; }

        public string MethodName { get; set; }

        public string[] Value { get; set; }

        public override string ToString()
        {
            return Kind + " - " + (Value == null ? "NV" : string.Join(" | ", Value));
        }
    }
}
