using RoslynScribe.Domain.Extensions;
using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    public class ScribeNodeData : IScribeNode
    {
        public ScribeNodeData(Guid id, string[] value)
        {
            Id = id;
            Guides = ScribeCommnetParser.Parse(value);
        }

        public Guid Id { get; set; }

        public ScribeGuides Guides { get; set; }

        public string Kind { get; set; }

        public MetaInfo MetaInfo { get; set; }

        public List<Guid> ChildNodeIds { get; set; } = new List<Guid>();
    }
}

