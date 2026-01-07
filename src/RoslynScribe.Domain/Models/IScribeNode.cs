namespace RoslynScribe.Domain.Models
{
    public interface IScribeNode
    {
        string Id { get; set; }

        //[JsonIgnore]
        //ScribeNode ParentNode { get; set; }
    }
}
