using RoslynScribe.Domain.Models;
using System;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Services
{
    public class ScribeBuilder
    {
        public static ScribeResult Merge(ScribeResult[] results)
        {
            
        }

        public static ScribeResult Rebuild(List<ScribeNode> nodes)
        {
            var trees = new List<ScribeTreeNode>(nodes.Count);
            foreach (var node in nodes)
            {
                trees.Add(BuildTree(node));
            }

            var nodeData = BuildNodeDataMap(nodes);

            return new ScribeResult { Trees = trees, Nodes = nodeData };
        }

        /// <summary>
        /// Rewrites ScribeNode tree into simplified ScribeTreeNode with only IDs and child relationships.
        /// </summary>
        /// <param name="node">The root ScribeNode from which to build the tree. Must not be null.</param>
        /// <returns>
        /// A ScribeTreeNode representing the root of the constructed tree, including all child node IDs.
        /// </returns>
        private static ScribeTreeNode BuildTree(ScribeNode node)
        {
            var id = node.Id;
            var treeNode = new ScribeTreeNode { Id = id };

            foreach (var child in node.ChildNodes)
            {
                treeNode.ChildNodes.Add(BuildTree(child));
            }

            return treeNode;
        }

        /// <summary>
        /// Builds a mapping from node identifiers to their corresponding node data for all nodes reachable from the
        /// specified root nodes.
        /// </summary>
        /// <remarks>
        /// Each node is included only once in the resulting map, even if reachable by multiple
        /// paths. The returned dictionary includes all nodes traversed from the roots, with their child node
        /// identifiers deduplicated.
        /// </remarks>
        /// <param name="documentRoots">The list of root nodes from which to traverse and collect node data. Cannot be null.</param>
        /// <returns>
        /// A dictionary mapping each node's identifier to its associated node data, including all nodes reachable from
        /// the provided roots.
        /// </returns>
        private static Dictionary<Guid, ScribeNodeData> BuildNodeDataMap(List<ScribeNode> documentRoots)
        {
            var byIdDict = new Dictionary<Guid, ScribeNode>();
            var visited = new HashSet<Guid>();
            var stack = new Stack<ScribeNode>(documentRoots);
            var userIds = new Dictionary<string, ScribeNode>();

            // Depth-first traversal to collect all nodes into byIdDict
            // suboptimal but simple
            while (stack.Count != 0)
            {
                var root = stack.Pop();
                var id = root.Id;
                if (!visited.Add(id))
                {
                    continue;
                }

                if (!byIdDict.ContainsKey(id))
                {
                    byIdDict.Add(id, root);
                }

                // collect data about userDefined IDs
                var userId = root.Guides?.UserDefinedId;
                if (!string.IsNullOrWhiteSpace(userId) && !userIds.ContainsKey(userId))
                {
                    userIds.Add(root.Guides.UserDefinedId, root);
                }

                foreach (var child in root.ChildNodes)
                {
                    stack.Push(child);
                }
            }

            // Rebuilds byIdDict into ScribeNodeData dict with deduplicated child IDs
            var result = new Dictionary<Guid, ScribeNodeData>(byIdDict.Count);
            foreach (var pair in byIdDict)
            {
                var node = pair.Value;
                var childNodeIds = new List<Guid>(node.ChildNodes.Count);
                var seenChildIds = new HashSet<Guid>();

                // Deduplicate child IDs
                // this should be done earlier in analysis, probably not needed here
                foreach (var child in node.ChildNodes)
                {
                    if (seenChildIds.Add(child.Id))
                    {
                        childNodeIds.Add(child.Id);
                    }
                }

                // Also add any user-defined ID references
                if (node.Guides != null)
                {
                    foreach (var userChildId in node.Guides.DestinationUserIds)
                    {
                        if (userIds.ContainsKey(userChildId))
                        {
                            var childNode = userIds[userChildId];
                            if (seenChildIds.Add(childNode.Id))
                            {
                                childNodeIds.Add(childNode.Id);
                            }
                        }
                    }
                }

                var nodeData = new ScribeNodeData(node.Id, node.Guides)
                {
                    Kind = node.Kind,
                    MetaInfo = node.MetaInfo,
                    ChildNodeIds = childNodeIds
                };

                result[pair.Key] = nodeData;
            }

            return result;
        }
    }
}
