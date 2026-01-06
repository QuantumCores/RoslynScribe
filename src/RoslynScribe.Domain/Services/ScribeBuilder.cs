using RoslynScribe.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RoslynScribe.Domain.Services
{
    public class ScribeBuilder
    {
        public static ScribeResult Merge(ScribeResult[] results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (results.Length == 0)
            {
                return new ScribeResult
                {
                    Trees = new List<ScribeTreeNode>(),
                    Nodes = new Dictionary<Guid, ScribeNodeData>()
                };
            }

            var mergedDataNodes = new Dictionary<Guid, ScribeNodeData>();
            var mergedUserIds = new Dictionary<string, ScribeNodeData>();
            var nodesWithDest = new Dictionary<Guid, ScribeNodeData>();

            // merge all nodes and collect user-defined IDs and nodes with destination IDs
            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];
                foreach (var pair in result.Nodes)
                {
                    var nodeId = pair.Key;
                    var nodeData = pair.Value;
                    if (!mergedDataNodes.ContainsKey(nodeId))
                    {
                        mergedDataNodes.Add(nodeId, nodeData);

                        var userId = nodeData.Guides?.UserDefinedId;
                        if (!string.IsNullOrWhiteSpace(userId) && !mergedUserIds.ContainsKey(userId))
                        {
                            if (!mergedUserIds.ContainsKey(userId))
                            {
                                mergedUserIds.Add(userId, nodeData);
                            }
                            else
                            {
                                ScribeConsole.Console.WriteLine($"Warning: Duplicate user-defined ID '{userId}' found on node {nodeId}. Skipping duplicate.", ConsoleColor.Yellow);
                            }
                        }

                        if (nodeData.Guides?.DestinationUserIds != null && nodeData.Guides?.DestinationUserIds.Length != 0)
                        {
                            nodesWithDest.Add(nodeId, nodeData);
                        }
                    }
                    else
                    {
                        ScribeConsole.Console.WriteLine($"Warning: Duplicate node ID {nodeId} found during merge. Skipping duplicate.", ConsoleColor.Yellow);
                    }
                }
            }

            // complement child relationships based on user-defined destination IDs
            foreach (var pair in nodesWithDest)
            {
                var nodeData = pair.Value;
                foreach (var userDestId in nodeData.Guides.DestinationUserIds)
                {
                    if (mergedUserIds.ContainsKey(userDestId))
                    {
                        var destNode = mergedUserIds[userDestId];
                        if (!HasChild(nodeData, destNode.Id))
                        {
                            nodeData.ChildNodeIds.Add(destNode.Id);
                        }
                    }
                }
            }

            var mergedTrees = new List<ScribeTreeNode>();
            // traverese all trees to rebuild tree structures
            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];

                foreach (var tree in result.Trees)
                {
                    mergedTrees.Add(tree);
                    var stack = new Stack<ScribeTreeNode>(new[] { tree });

                    // ensure all child relationships in tree are updated
                    while (stack.Count != 0)
                    {
                        var root = stack.Pop();
                        var id = root.Id;

                        // we add child nodes in the beginning to avoid double processing of newly added nodes
                        foreach (var child in root.ChildNodes)
                        {
                            stack.Push(child);
                        }

                        // add any missing child nodes based on destination user IDs
                        if (nodesWithDest.ContainsKey(id))
                        {
                            var nodeWithDest = nodesWithDest[id];
                            foreach (var userDestId in nodeWithDest.Guides.DestinationUserIds)
                            {
                                if (string.IsNullOrWhiteSpace(userDestId))
                                {
                                    continue;
                                }

                                if (mergedUserIds.ContainsKey(userDestId))
                                {
                                    var destNode = mergedUserIds[userDestId];
                                    if (!HasChild(root, destNode.Id))
                                    {
                                        root.ChildNodes.Add(new ScribeTreeNode { Id = destNode.Id });
                                    }

                                    if (!HasChild(nodeWithDest, destNode.Id))
                                    {
                                        nodeWithDest.ChildNodeIds.Add(destNode.Id);
                                    }
                                }
                            }
                        }

                        // add missing child nodes to treeNode based on node child ids
                        if (mergedDataNodes.ContainsKey(id))
                        {
                            var nodeData = mergedDataNodes[id];
                            var idsCopy = nodeData.ChildNodeIds.ToList();
                            foreach (var childId in root.ChildNodes)
                            {
                                idsCopy.Remove(childId.Id);
                            }

                            foreach (var leftId in idsCopy)
                            {
                                root.ChildNodes.Add(new ScribeTreeNode { Id = leftId });
                            }
                        }
                    }
                }
            }

            return new ScribeResult { Nodes = mergedDataNodes, Trees = mergedTrees };
        }

        public static ScribeResult Rebuild(List<ScribeNode> nodes)
        {
            // node data map has to be built first to also capture userDefined relations
            var nodeData = BuildNodeDataMap(nodes);

            var trees = new List<ScribeTreeNode>(nodes.Count);
            foreach (var node in nodes)
            {
                trees.Add(BuildTree(node));
            }

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
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    if (!userIds.ContainsKey(userId))
                    {
                        userIds.Add(root.Guides.UserDefinedId, root);
                    }
                    else
                    {
                        ScribeConsole.Console.WriteLine($"Warning: Duplicate user-defined ID '{userId}' found on node {id}. Skipping duplicate.", ConsoleColor.Yellow);
                    }

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
                                node.ChildNodes.Add(childNode);
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

        private static ScribeTreeNode CloneTree(ScribeTreeNode node)
        {
            var clone = new ScribeTreeNode { Id = node.Id };
            foreach (var child in node.ChildNodes)
            {
                clone.ChildNodes.Add(CloneTree(child));
            }

            return clone;
        }

        private static void IndexTreeNodes(ScribeTreeNode node, Dictionary<Guid, List<ScribeTreeNode>> index)
        {
            List<ScribeTreeNode> list;
            if (!index.TryGetValue(node.Id, out list))
            {
                list = new List<ScribeTreeNode>();
                index.Add(node.Id, list);
            }

            list.Add(node);

            foreach (var child in node.ChildNodes)
            {
                IndexTreeNodes(child, index);
            }
        }

        private static bool HasChild(ScribeTreeNode node, Guid childId)
        {
            for (var i = 0; i < node.ChildNodes.Count; i++)
            {
                if (node.ChildNodes[i].Id == childId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasChild(ScribeNodeData node, Guid childId)
        {
            for (var i = 0; i < node.ChildNodeIds.Count; i++)
            {
                if (node.ChildNodeIds[i] == childId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
