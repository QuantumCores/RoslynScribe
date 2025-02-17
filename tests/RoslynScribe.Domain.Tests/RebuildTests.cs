using NUnit.Framework;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System.Collections.Generic;
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
    }
}
