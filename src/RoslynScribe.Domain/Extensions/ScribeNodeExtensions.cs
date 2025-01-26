using RoslynScribe.Domain.Models;

namespace RoslynScribe.Domain.Extensions
{
    public static class ScribeNodeExtensions
    {
        public static bool IsEquivalent(this ScribeNode leftNode, ScribeNode rightNode)
        {
            return Traverse(leftNode, rightNode);
        }

        public static bool IsTheSame(this ScribeNode leftNode, ScribeNode rightNode)
        {
            return Traverse(leftNode, rightNode, true);
        }

        private static bool Traverse(ScribeNode leftNode, ScribeNode rightNode, bool compareMetaInfo = false)
        {
            if (leftNode.ChildNodes.Count != rightNode.ChildNodes.Count)
            {
                return false;
            }

            if (leftNode.Value != null && rightNode.Value != null)
            {
                if (leftNode.Value.Length != rightNode.Value.Length)
                {
                    return false;
                }

                for (int i = 0; i < leftNode.Value.Length; i++)
                {
                    if (leftNode.Value[i] != rightNode.Value[i])
                    {
                        return false;
                    }
                }
            }

            if (leftNode.Value == null && rightNode.Value != null ||
                leftNode.Value != null && rightNode.Value == null)
            {
                return false;
            }

            if (compareMetaInfo)
            {
                if (!leftNode.MetaInfo.Equals(rightNode.MetaInfo))
                {
                    return false;
                }
            }

            for (int i = 0; i < leftNode.ChildNodes.Count; i++)
            {
                if (!Traverse(leftNode.ChildNodes[i], rightNode.ChildNodes[i], compareMetaInfo))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
