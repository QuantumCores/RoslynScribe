using RoslynScribe.Domain.Models;
using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Extensions
{
    public static class ScribeNodeExtensions
    {
        public static (bool Result, string Text) IsEquivalent(this ScribeNode actual, ScribeNode expected)
        {
            return Traverse(actual, expected);
        }

        public static (bool Result, string Text) IsTheSame(this ScribeNode actual, ScribeNode expected)
        {
            return Traverse(actual, expected, true);
        }

        private static (bool Result, string Text) Traverse(ScribeNode actual, ScribeNode expected, bool compareMetaInfo = false)
        {
            if (actual.ChildNodes.Count != expected.ChildNodes.Count)
            {
                return (false, Text(actual, expected, nameof(ScribeNode.ChildNodes) + "." + nameof(List<ScribeNode>.Count), $"Actual: {actual.ChildNodes.Count} vs expected: {expected.ChildNodes.Count}"));
            }

            if (actual.Value != null && expected.Value != null)
            {
                if (actual.Value.Length != expected.Value.Length)
                {
                    return (false, Text(actual, expected, nameof(Array.Length), $"Actual: {actual.Value.Length} vs expected: {expected.Value.Length}"));
                }

                for (int i = 0; i < actual.Value.Length; i++)
                {
                    if (actual.Value[i] != expected.Value[i])
                    {
                        return (false, Text(actual, expected, nameof(ScribeNode.Value), $"At Value[{i}]"));
                    }
                }
            }

            if (actual.Value == null && expected.Value != null ||
                actual.Value != null && expected.Value == null)
            {
                return (false, Text(actual, expected, nameof(ScribeNode.Value), "One is null")); ;
            }

            if (compareMetaInfo)
            {
                if (!actual.MetaInfo.Equals(expected.MetaInfo))
                {
                    return (false, Text(actual, expected, nameof(ScribeNode.MetaInfo), $"Actual Id: {actual.MetaInfo.Identifier}")); ;
                }
            }

            for (int i = 0; i < actual.ChildNodes.Count; i++)
            {

                var traverseResult = Traverse(actual.ChildNodes[i], expected.ChildNodes[i], compareMetaInfo);
                if (!traverseResult.Result)
                {
                    return traverseResult;
                }
            }

            return (true, null);
        }

        private static string Text(ScribeNode leftNode, ScribeNode rightNode, string property, string additionalText)
        {
            return $"Difference found in property '{property}' between nodes {leftNode.Value?[0]} and {rightNode.Value?[0]}. {additionalText}.";
        }
    }
}
