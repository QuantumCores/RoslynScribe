using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Models
{
    public class ScribeResult
    {
        public List<ScribeNode> Trees { get; set; }

        public Dictionary<Guid, ScribeNode> Nodes { get; set; }
    }
}
