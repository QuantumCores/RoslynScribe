using System;

namespace RoslynScribe.Domain.Models
{
    public interface IScribeNode
    {
        Guid Id { get; set; }

        //[JsonIgnore]
        //ScribeNode ParentNode { get; set; }
    }
}
