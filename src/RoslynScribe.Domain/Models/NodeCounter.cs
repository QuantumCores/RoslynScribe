namespace RoslynScribe.Domain.Models
{
    internal class NodeCounter
    {
        internal int Count { get; set; }

        internal ScribeNode Node { get; }

        public NodeCounter(ScribeNode node)
        {
            Count = 1;
            Node = node;
        }
    }
}
