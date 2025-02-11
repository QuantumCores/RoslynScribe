using System.Text.Json.Serialization;

namespace RoslynScribe.Domain.Models
{
    public interface IScribeNode
    {
        int Id { get; set; }

        [JsonIgnore]
        ScribeNode ParentNode { get; set; }
    }
}
