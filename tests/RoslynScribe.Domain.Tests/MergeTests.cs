using System;
using System.Collections.Generic;
using NUnit.Framework;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;

namespace RoslynScribe.Domain.Tests
{
    public class MergeTests
    {
        [Test]
        public void MyMerge_connects_external_child_nodes_in_tree()
        {
            var parentId = Guid.NewGuid();
            var externalId = Guid.NewGuid();

            var result1 = new ScribeResult
            {
                Trees = new List<ScribeTreeNode>
                {
                    new ScribeTreeNode { Id = parentId }
                },
                Nodes = new Dictionary<Guid, ScribeNodeData>
                {
                    {
                        parentId,
                        new ScribeNodeData(parentId, new ScribeGuides())
                        {
                            ChildNodeIds = new List<Guid> { externalId }
                        }
                    }
                }
            };

            var result2 = new ScribeResult
            {
                Trees = new List<ScribeTreeNode>
                {
                    new ScribeTreeNode { Id = externalId }
                },
                Nodes = new Dictionary<Guid, ScribeNodeData>
                {
                    { externalId, new ScribeNodeData(externalId, new ScribeGuides()) }
                }
            };

            var merged = ScribeBuilder.MyMerge(new[] { result1, result2 });
            var parentNode = FindNode(merged.Trees, parentId);

            Assert.That(parentNode, Is.Not.Null);
            Assert.That(HasChild(parentNode, externalId), Is.True);
            Assert.That(merged.Nodes[parentId].ChildNodeIds.Contains(externalId), Is.True);
        }

        [Test]
        public void MyMerge_connects_external_destination_user_ids_in_tree()
        {
            var parentId = Guid.NewGuid();
            var targetId = Guid.NewGuid();
            var targetUserId = "target-user-id";

            var result1 = new ScribeResult
            {
                Trees = new List<ScribeTreeNode>
                {
                    new ScribeTreeNode { Id = parentId }
                },
                Nodes = new Dictionary<Guid, ScribeNodeData>
                {
                    {
                        parentId,
                        new ScribeNodeData(parentId, new ScribeGuides
                        {
                            DestinationUserIds = new[] { targetUserId }
                        })
                    }
                }
            };

            var result2 = new ScribeResult
            {
                Trees = new List<ScribeTreeNode>
                {
                    new ScribeTreeNode { Id = targetId }
                },
                Nodes = new Dictionary<Guid, ScribeNodeData>
                {
                    {
                        targetId,
                        new ScribeNodeData(targetId, new ScribeGuides
                        {
                            UserDefinedId = targetUserId
                        })
                    }
                }
            };

            var merged = ScribeBuilder.MyMerge(new[] { result1, result2 });
            var parentNode = FindNode(merged.Trees, parentId);

            Assert.That(parentNode, Is.Not.Null);
            Assert.That(HasChild(parentNode, targetId), Is.True);
            Assert.That(merged.Nodes[parentId].ChildNodeIds.Contains(targetId), Is.True);
        }

        [Test]
        public void MyMerge_ignores_unknown_destination_user_ids()
        {
            var parentId = Guid.NewGuid();

            var result1 = new ScribeResult
            {
                Trees = new List<ScribeTreeNode>
                {
                    new ScribeTreeNode { Id = parentId }
                },
                Nodes = new Dictionary<Guid, ScribeNodeData>
                {
                    {
                        parentId,
                        new ScribeNodeData(parentId, new ScribeGuides
                        {
                            DestinationUserIds = new[] { "missing" }
                        })
                    }
                }
            };

            var merged = ScribeBuilder.MyMerge(new[] { result1 });
            var parentNode = FindNode(merged.Trees, parentId);

            Assert.That(parentNode, Is.Not.Null);
            Assert.That(parentNode.ChildNodes.Count, Is.EqualTo(0));
            Assert.That(merged.Nodes[parentId].ChildNodeIds.Count, Is.EqualTo(0));
        }

        [Test]
        public void MyMerge_returns_empty_result_for_empty_input()
        {
            var merged = ScribeBuilder.MyMerge(new ScribeResult[0]);

            Assert.That(merged, Is.Not.Null);
            Assert.That(merged.Trees, Is.Not.Null);
            Assert.That(merged.Nodes, Is.Not.Null);
            Assert.That(merged.Trees.Count, Is.EqualTo(0));
            Assert.That(merged.Nodes.Count, Is.EqualTo(0));
        }

        private static ScribeTreeNode FindNode(List<ScribeTreeNode> trees, Guid id)
        {
            foreach (var tree in trees)
            {
                var node = FindNode(tree, id);
                if (node != null)
                {
                    return node;
                }
            }

            return null;
        }

        private static ScribeTreeNode FindNode(ScribeTreeNode node, Guid id)
        {
            if (node.Id == id)
            {
                return node;
            }

            foreach (var child in node.ChildNodes)
            {
                var found = FindNode(child, id);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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
    }
}
