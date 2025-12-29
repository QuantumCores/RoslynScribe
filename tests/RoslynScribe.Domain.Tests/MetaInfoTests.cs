using NUnit.Framework;
using RoslynScribe.Domain.Extensions;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.Tests
{
    public class MetaInfoTests
    {
        private string _expectedDirectory;
        private static bool overriddeResults = true;

        private static void OverrideResults(string path, ScribeNode result)
        {
            if (overriddeResults)
            {         
                File.WriteAllText(path, JsonSerializer.Serialize(result, new JsonSerializerOptions() { WriteIndented = true }));
            }
        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            await TestFixture.Prepare();
            _expectedDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
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
            var path = Path.Combine(_expectedDirectory, "ExpectedResults", "S001_BasicComments.json");
            var json = File.ReadAllText(path);
            var expected = JsonSerializer.Deserialize<ScribeNode>(json);

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S001_BasicComments.cs", new AdcConfig { Types = Array.Empty<AdcType>() });
            OverrideResults(path, result);

            // Assert
            Assert.IsTrue(result.IsTheSame(expected).Result);
        }

        [Test]
        public async Task S002_returns_valid_tree()
        {
            // Arrange
            var path = Path.Combine(_expectedDirectory, "ExpectedResults", "S002_MultiLineComments.json");
            var json = File.ReadAllText(path);
            var expected = JsonSerializer.Deserialize<ScribeNode>(json);

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S002_MultiLineComments.cs", new AdcConfig { Types = Array.Empty<AdcType>() });
            OverrideResults(path, result);

            // Assert
            Assert.IsTrue(result.IsTheSame(expected).Result);
        }

        [Test]
        public async Task S003_returns_valid_tree()
        {
            // Arrange
            var path = Path.Combine(_expectedDirectory, "ExpectedResults", "S003_CommentFromLocalMethod.json");
            var json = File.ReadAllText(path);
            var expected = JsonSerializer.Deserialize<ScribeNode>(json);

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig { Types = Array.Empty<AdcType>() });
            OverrideResults(path, result);

            // Assert
            Assert.IsTrue(result.IsTheSame(expected).Result);
        }

        [Test]
        public async Task S004_returns_valid_tree()
        {
            // Arrange
            var path = Path.Combine(_expectedDirectory, "ExpectedResults", "S004_CommentFromOtherClassMethod.json");
            var json = File.ReadAllText(path);
            var expected = JsonSerializer.Deserialize<ScribeNode>(json);

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S004_CommentFromOtherClassMethod.cs", new AdcConfig { Types = Array.Empty<AdcType>() });
            OverrideResults(path, result);

            // Assert
            Assert.IsTrue(result.IsTheSame(expected).Result);
        }


        [Test]
        public async Task S005_returns_valid_tree()
        {
            // Arrange
            var path = Path.Combine(_expectedDirectory, "ExpectedResults", "S005_IfStatementBlockComment.json");
            var json = File.ReadAllText(path);
            var expected = JsonSerializer.Deserialize<ScribeNode>(json);

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S005_IfStatementBlockComment.cs", new AdcConfig { Types = Array.Empty<AdcType>() });
            OverrideResults(path, result);

            // Assert
            Assert.IsTrue(result.IsTheSame(expected).Result);
        }

        [Test]
        public async Task S006_returns_valid_tree()
        {
            // Arrange
            var path = Path.Combine(_expectedDirectory, "ExpectedResults", "S006_ThirdPartyLibrary.json");
            var json = File.ReadAllText(path);
            var expected = JsonSerializer.Deserialize<ScribeNode>(json);

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S006_ThirdPartyLibrary.cs", new AdcConfig { Types = Array.Empty<AdcType>() });
            OverrideResults(path, result);

            // Assert
            Assert.IsTrue(result.IsTheSame(expected).Result);
        }

        [Test]
        public async Task S007_returns_valid_tree()
        {
            // Arrange
            var path = Path.Combine(_expectedDirectory, "ExpectedResults", "S007_CallLambda.json");
            var json = File.ReadAllText(path);
            var expected = JsonSerializer.Deserialize<ScribeNode>(json);

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S007_CallLambda.cs", new AdcConfig { Types = Array.Empty<AdcType>() });
            OverrideResults(path, result);

            // Assert
            Assert.IsTrue(result.IsTheSame(expected).Result);
        }


        [Test]
        public async Task S008_returns_valid_tree()
        {
            // Arrange
            var path = Path.Combine(_expectedDirectory, "ExpectedResults", "S008_CommentFromOtherProject.json");
            var json = File.ReadAllText(path);
            var expected = JsonSerializer.Deserialize<ScribeNode>(json);

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S008_CommentFromOtherProject.cs", new AdcConfig { Types = Array.Empty<AdcType>() });
            OverrideResults(path, result);

            // Assert
            Assert.IsTrue(result.IsTheSame(expected).Result);
        }
    }
}
