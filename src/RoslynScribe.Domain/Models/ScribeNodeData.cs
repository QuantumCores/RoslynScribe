using RoslynScribe.Domain.Extensions;
using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    public class ScribeNodeData : IScribeNode
    {
        public ScribeNodeData(string id, ScribeGuides guides)
        {
            Id = id;
            Guides = guides;
        }

        public string Id { get; set; }

        public ScribeGuides Guides { get; set; }

        public string Kind { get; set; }

        public MetaInfo MetaInfo { get; set; }

        public List<string> ChildNodeIds { get; set; } = new List<string>();
    }
}
