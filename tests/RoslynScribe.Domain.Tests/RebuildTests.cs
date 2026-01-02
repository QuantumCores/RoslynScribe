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
        private string _expectedDirectory;

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
        public async Task S003_finds_duplicated_nodes()
        {
            // Arrange
            var tree = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());

            // Act
            var result = ScribeAnalyzer.FindDuplicatedNodes(new List<ScribeNode> { tree });

            // Assert
            Assert.IsTrue(result.Count == 2);
        }

        [Test]
        public async Task S003_returns_valid_tree()
        {
            // Arrange
            var tree = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());

            // Act
            var result = ScribeAnalyzer.Rebuild(new List<ScribeNode> { tree });

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
        public async Task RegisterNodes_is_deterministic_for_same_document()
        {
            var tree1 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());
            var tree2 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());

            var map1 = ScribeAnalyzer.FindDuplicatedNodes(new List<ScribeNode> { tree1 });
            var map2 = ScribeAnalyzer.FindDuplicatedNodes(new List<ScribeNode> { tree2 });

            CollectionAssert.AreEquivalent(map1.Keys, map2.Keys);
            CollectionAssert.AreEquivalent(map1.Values.Select(x => x.Id), map2.Values.Select(x => x.Id));
        }

        [Test]
        public async Task Rebuild_targets_are_deterministic()
        {
            var tree1 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());
            var tree2 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());

            var rebuilt1 = ScribeAnalyzer.Rebuild(new List<ScribeNode> { tree1 });
            var rebuilt2 = ScribeAnalyzer.Rebuild(new List<ScribeNode> { tree2 });

            CollectionAssert.AreEquivalent(rebuilt1.Nodes.Keys, rebuilt2.Nodes.Keys);
            CollectionAssert.AreEqual(GetTreeIds(rebuilt1.Trees), GetTreeIds(rebuilt2.Trees));
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
