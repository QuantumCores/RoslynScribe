using RoslynScribe.Domain.Models;
using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Extensions
{
    public static class ScribeNodeExtensions
    {
        public static (bool Result, string Text) IsEquivalent(this ScribeNode leftNode, ScribeNode rightNode)
        {
            return Traverse(leftNode, rightNode);
        }

        public static (bool Result, string Text) IsTheSame(this ScribeNode leftNode, ScribeNode rightNode)
        {
            return Traverse(leftNode, rightNode, true);
        }

        private static (bool Result, string Text) Traverse(ScribeNode leftNode, ScribeNode rightNode, bool compareMetaInfo = false)
        {
            if (leftNode.ChildNodes.Count != rightNode.ChildNodes.Count)
            {
                return (false, Text(leftNode, rightNode, nameof(List<ScribeNode>.Count)));
            }

            if (leftNode.Value != null && rightNode.Value != null)
            {
                if (leftNode.Value.Length != rightNode.Value.Length)
                {
                    return (false, Text(leftNode, rightNode, nameof(Array.Length)));
                }

                for (int i = 0; i < leftNode.Value.Length; i++)
                {
                    if (leftNode.Value[i] != rightNode.Value[i])
                    {
                        return (false, Text(leftNode, rightNode, nameof(ScribeNode.Value)));
                    }
                }
            }

            if (leftNode.Value == null && rightNode.Value != null ||
                leftNode.Value != null && rightNode.Value == null)
            {
                return (false, Text(leftNode, rightNode, nameof(ScribeNode.Value))); ;
            }

            if (compareMetaInfo)
            {
                if (!leftNode.MetaInfo.Equals(rightNode.MetaInfo))
                {
                    return (false, Text(leftNode, rightNode, nameof(ScribeNode.MetaInfo))); ;
                }
            }

            for (int i = 0; i < leftNode.ChildNodes.Count; i++)
            {

                var traverseResult = Traverse(leftNode.ChildNodes[i], rightNode.ChildNodes[i], compareMetaInfo);
                if (!traverseResult.Result)
                {
                    return traverseResult;
                }
            }

            return (true, null);
        }

        private static string Text(ScribeNode leftNode, ScribeNode rightNode, string property)
        {
            return $"Difference found in property '{property}' between nodes {leftNode.Value?[0]} and {rightNode.Value?[0]}.";
        }
    }
}
