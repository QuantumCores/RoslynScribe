using System;
using NUnit.Framework;
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
        public async Task S001_returns_valid_tree()
        {
            // Arrange
            var tree = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs");

            // Act
            var result = ScribeAnalyzer.RegisterNodes(new List<ScribeNode> { tree });

            // Assert
            Assert.IsTrue(result.Count == 2);
        }

        [Test]
        public async Task S003_returns_valid_tree()
        {
            // Arrange
            var tree = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs");

            // Act
            var result = ScribeAnalyzer.Rebuild(new List<ScribeNode> { tree });

            // Assert
            Assert.That(result.Nodes.Count == 2);
        }

        [Test]
        public async Task RegisterNodes_is_deterministic_for_same_document()
        {
            var tree1 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs");
            var tree2 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs");

            var map1 = ScribeAnalyzer.RegisterNodes(new List<ScribeNode> { tree1 });
            var map2 = ScribeAnalyzer.RegisterNodes(new List<ScribeNode> { tree2 });

            CollectionAssert.AreEquivalent(map1.Keys, map2.Keys);
            CollectionAssert.AreEquivalent(map1.Values.Select(x => x.Id), map2.Values.Select(x => x.Id));
        }

        [Test]
        public async Task Rebuild_targets_are_deterministic()
        {
            var tree1 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs");
            var tree2 = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs");

            var rebuilt1 = ScribeAnalyzer.Rebuild(new List<ScribeNode> { tree1 });
            var rebuilt2 = ScribeAnalyzer.Rebuild(new List<ScribeNode> { tree2 });

            CollectionAssert.AreEquivalent(rebuilt1.Nodes.Keys, rebuilt2.Nodes.Keys);
            CollectionAssert.AreEquivalent(GetTargetIds(rebuilt1.Trees), GetTargetIds(rebuilt2.Trees));
        }

        private static List<Guid> GetTargetIds(List<ScribeNode> trees)
        {
            var targets = new List<Guid>();
            void Traverse(ScribeNode node)
            {
                if (node.TargetNodeId.HasValue)
                {
                    targets.Add(node.TargetNodeId.Value);
                }

                foreach (var child in node.ChildNodes)
                {
                    Traverse(child);
                }
            }

            foreach (var tree in trees)
            {
                Traverse(tree);
            }

            return targets;
        }
    }
}
