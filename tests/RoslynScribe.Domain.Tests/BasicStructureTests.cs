using NUnit.Framework;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using RoslynScribe.Domain.Extensions;
using System.Text.Json;

namespace RoslynScribe.Domain.Tests
{
    public class BasicStructureTests
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
        public async Task S001_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { "// S001 namespace comment" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ "// S001 BasicTestClass" },
                                ChildNodes = new List<ScribeNode> {
                                    new ScribeNode
                                    {
                                        Value = new string[]{ "// S001 BasicMethod" },
                                        ChildNodes = new List<ScribeNode>
                                        {
                                            new ScribeNode
                                            {
                                                Value = new string[]{ "// S001 result" },
                                                ChildNodes = new List<ScribeNode> { },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            // Act            
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S001_BasicComments.cs");

             var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.IsTrue(result.IsEquivalent(expected));
        }

        [Test]
        public async Task S002_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { "// S002 namespace comment", "// S002 second namespace comment" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ "// S002 class comment", "// S002 second class comment" },
                                ChildNodes = new List<ScribeNode> {
                                    new ScribeNode
                                    {
                                        Value = new string[]{ "// S002 BasicMethod", "// S002 second method comment" },
                                        ChildNodes = new List<ScribeNode>
                                        {
                                            new ScribeNode
                                            {
                                                Value = new string[]{ "// S002 result", "// S002 seconod result" },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S002_MultiLineComments.cs");

            // var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.IsTrue(result.IsEquivalent(expected));
        }

        [Test]
        public async Task S003_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[]{ "// S003 basicMethod" },
                        ChildNodes = new List<ScribeNode> {
                            new ScribeNode
                            {
                                Value = new string[]{ "// S003 basic result" },
                            },
                        },
                    },
                    new ScribeNode
                    {
                        Value = new string[] { "// S003 second method" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ "// S003 call basicMethod" },                                
                                ChildNodes = new List<ScribeNode> {
                                    new ScribeNode
                                    {
                                        Value = new string[]{ "// S003 basicMethod" },
                                        ChildNodes = new List<ScribeNode> {
                                            new ScribeNode
                                            {
                                                Value = new string[]{ "// S003 basic result" },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs");

            // var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.IsTrue(result.IsEquivalent(expected));
        }

        [Test]
        public async Task S004_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { "// S004 return addition" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ "// S004 Nodes shared comment get logic data " },
                            },
                            new ScribeNode
                            {
                                Value = new string[]{ "// Logic Add two numbers" },
                                ChildNodes = new List<ScribeNode> {
                                    new ScribeNode
                                    {
                                        Value = new string[]{ "// Logic This is add result" },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S004_CommentFromOtherClassMethod.cs");

            // var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.IsTrue(result.IsEquivalent(expected));
        }

        [Test]
        public async Task S005_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { "// Check condition value" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ "// S005 If condition is one then 1" },
                            },
                            new ScribeNode
                            {
                                Value = new string[]{ "// S005 If condition is two then 2" },
                            },
                            new ScribeNode
                            {
                                Value = new string[]{ "// S005 If condition is other then -1" },
                            },
                        },
                    },
                }
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S005_IfStatementBlockComment.cs");

            // var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.IsTrue(result.IsEquivalent(expected));
        }

        [Test]
        public async Task S006_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { "// S006 call external method to add days" },
                    },
                }
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S006_ThirdPartyLibrary.cs");

            // var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.IsTrue(result.IsEquivalent(expected));
        }

        [Test]
        public async Task S007_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { "// S007 prepare lambda" },
                    },
                    new ScribeNode
                    {
                        Value = new string[] { "// S007 add one in lambda expression" },
                    },
                }
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S007_CallLambda.cs");

            // var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.IsTrue(result.IsEquivalent(expected));
        }

        [Test]
        public async Task S008_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { "// S008 Nodes shared comment" },                        
                    },
                    new ScribeNode
                    {
                        Value = new string[]{ "// OtherLogic Multiply two numbers" },
                        ChildNodes = new List<ScribeNode> {
                            new ScribeNode
                            {
                                Value = new string[]{ "// OtherLogic This is multiplication result" },
                            },
                        },
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S008_CommentFromOtherProject.cs");

            // var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.IsTrue(result.IsEquivalent(expected));
        }
    }
}
