using NUnit.Framework;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.Tests
{
    public class DictionaryTests
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

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S001_BasicComments.cs");
            var dict = ScribeAnalyzer.Rebuild(new List<ScribeNode> { result });

            // Assert
            Assert.IsTrue(dict.Count == 3);
        }
    }
}
