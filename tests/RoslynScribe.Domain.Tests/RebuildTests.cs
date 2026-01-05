using System;
using NUnit.Framework;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.Tests
{
    public class RebuildTests
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            await TestFixture.Prepare();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestFixture.StaticDispose();
        }

        [Test]
        public async Task S003_returns_valid_tree()
        {
            // Arrange
            var tree = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());

            // Act
            var result = ScribeBuilder.Rebuild(new List<ScribeNode> { tree });

            // Assert
            Assert.That(result.Nodes.Count, Is.GreaterThan(0));
            Assert.That(result.Trees.Count, Is.EqualTo(1));

            foreach (var node in result.Nodes.Values)
            {
                Assert.That(node.ChildNodeIds, Is.Not.Null);
                Assert.That(node.ChildNodeIds.All(result.Nodes.ContainsKey), Is.True);
            }
        }

        [Test]
        public async Task Rebuild_targets_are_deterministic()
        {
            var tree1 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());
            var tree2 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());

            var rebuilt1 = ScribeBuilder.Rebuild(new List<ScribeNode> { tree1 });
            var rebuilt2 = ScribeBuilder.Rebuild(new List<ScribeNode> { tree2 });

            CollectionAssert.AreEquivalent(rebuilt1.Nodes.Keys, rebuilt2.Nodes.Keys);
            CollectionAssert.AreEqual(GetTreeIds(rebuilt1.Trees), GetTreeIds(rebuilt2.Trees));
        }

        [Test]
        public async Task Rebuild_has_recursion_protection()
        {
            // Arrange
            var tree = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S010_InfiniteRecursion.cs", new AdcConfig());

            // Act
            var rebuilt = ScribeBuilder.Rebuild(new List<ScribeNode> { tree });

            // Assert
            Assert.That(rebuilt.Nodes.Count, Is.GreaterThan(0));
        }

        [Test]
        public async Task Rebuild_includes_user_defined_childs()
        {
            // Arrange
            var userDefinedId = "user-defined-child";
            var child1Guid = Guid.NewGuid();
            var child2Guid = Guid.NewGuid();
            var tree = new ScribeNode
            {
                Id = Guid.NewGuid(),
                Kind = "Root",
                ChildNodes = new List<ScribeNode>
                {
                    new ScribeNode
                    {
                        Id = child1Guid,
                        Kind = "Child1",
                        ChildNodes = new List<ScribeNode>(),
                        Guides = new ScribeGuides
                        {
                            UserDefinedId = userDefinedId,
                        }

                    },
                    new ScribeNode
                    {
                        Id = child2Guid,
                        Kind = "Child2",
                        ChildNodes = new List<ScribeNode>(),
                        Guides = new ScribeGuides
                        {
                            DestinationUserIds = new[] { userDefinedId }
                        }
                    }
                }
            };

            // Act
            var rebuilt = ScribeBuilder.Rebuild(new List<ScribeNode> { tree });

            // Assert
            Assert.That(rebuilt.Nodes.Count, Is.EqualTo(3));
            Assert.That(rebuilt.Nodes[child2Guid].ChildNodeIds[0] == child1Guid);
            Assert.That(rebuilt.Trees[0].ChildNodes.Select(x => x.Id), Is.EquivalentTo(new Guid[] { child1Guid, child2Guid }));
            // verify that tree is updated and child2 has child1 as its child node
            Assert.That(rebuilt.Trees[0].ChildNodes[1].ChildNodes[0].Id, Is.EqualTo(child1Guid));
        }

        private static List<Guid> GetTreeIds(List<ScribeTreeNode> trees)
        {
            var ids = new List<Guid>();
            void Traverse(ScribeTreeNode node)
            {
                ids.Add(node.Id);

                foreach (var child in node.ChildNodes)
                {
                    Traverse(child);
                }
            }

            foreach (var tree in trees)
            {
                Traverse(tree);
            }

            return ids;
        }
    }
}
