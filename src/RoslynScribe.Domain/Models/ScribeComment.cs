namespace RoslynScribe.Domain.Models
{
    public class ScribeComment
    {
        public string[] Comments { get; set; }

        public ScribeGuides Guide { get; set; }
    }
}
