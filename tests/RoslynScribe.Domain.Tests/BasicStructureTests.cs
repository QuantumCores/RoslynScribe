using NUnit.Framework;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System;
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S001 namespace comment`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S001 BasicTestClass`]" },
                                ChildNodes = new List<ScribeNode> {
                                    new ScribeNode
                                    {
                                        Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S001 BasicMethod`]" },
                                        ChildNodes = new List<ScribeNode>
                                        {
                                            new ScribeNode
                                            {
                                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S001 result`]" },
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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S001_BasicComments.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S002 namespace comment`]", $"// {ScribeAnalyzer.CommentLabel}[T:`S002 second namespace comment`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S002 class comment`]", $"// {ScribeAnalyzer.CommentLabel}[T:`S002 second class comment`]" },
                                ChildNodes = new List<ScribeNode> {
                                    new ScribeNode
                                    {
                                        Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S002 BasicMethod`]", $"// {ScribeAnalyzer.CommentLabel}[T:`S002 second method comment`]" },
                                        ChildNodes = new List<ScribeNode>
                                        {
                                            new ScribeNode
                                            {
                                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S002 result`]", $"// {ScribeAnalyzer.CommentLabel}[T:`S002 seconod result`]" },
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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S002_MultiLineComments.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
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
                        Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S003 basicMethod`]" },
                        ChildNodes = new List<ScribeNode> {
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S003 basic result`]" },
                            },
                        },
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S003 second method`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S003 call basicMethod`]" },
                                ChildNodes = new List<ScribeNode> {
                                    new ScribeNode
                                    {
                                        Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S003 basicMethod`]" },
                                        ChildNodes = new List<ScribeNode> {
                                            new ScribeNode
                                            {
                                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S003 basic result`]" },
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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            //var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S004 return addition`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S004 Nodes shared comment get logic data`]" },
                            },
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`Logic Add two numbers`,L:`2`]" },
                                ChildNodes = new List<ScribeNode> {
                                    new ScribeNode
                                    {
                                        Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`Logic This is add result`]" },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S004_CommentFromOtherClassMethod.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`Check condition value`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S005 If condition is one then 1`]" },
                            },
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S005 If condition is two then 2`]" },
                            },
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`S005 If condition is other then -1`]" },
                            },
                        },
                    },
                }
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S005_IfStatementBlockComment.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S006 call external method to add days`]" },
                    },
                }
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S006_ThirdPartyLibrary.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S007 prepare lambda`]" },
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S007 add one in lambda expression`]" },
                    },
                }
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S007_CallLambda.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S008 Nodes shared comment`]" },
                    },
                    new ScribeNode
                    {
                        Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`OtherLogic Multiply two numbers`,L:`2`,D:`IrrelevantDescription`]" },
                        ChildNodes = new List<ScribeNode> {
                            new ScribeNode
                            {
                                Value = new string[]{ $"// {ScribeAnalyzer.CommentLabel}[T:`OtherLogic This is multiplication result`]" },
                            },
                        },
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S008_CommentFromOtherProject.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }

        [Test]
        public async Task S009_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S009 Method A calls C`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S009 Method D called by C`]" },
                            },
                        }
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S009 Method B calls C`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S009 Method D called by C`]" },
                            },
                        }
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S009 Method D called by C`]" },
                    },

                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S009_TwoMethodCalls.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }

        [Test]
        public async Task S010_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S010 Recursive start`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S010 Recursive method`]" },
                                ChildNodes = new List<ScribeNode>()
                                {
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S010 Call recursive method`]" },
                                    },
                                }
                            },
                        }
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S010 Recursive method`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S010 Call recursive method`]" },
                            },
                        }

                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S010_InfiniteRecursion.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }

        [Test]
        public async Task S011_returns_valid_tree()
        {
            // Arrange
            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S011 This is class`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S011 This is class property`]" },
                            },
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S011 This is class method`]" },
                            },
                        }
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S011 This is interface`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S011 This is interface property`]" },
                            },
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S011 This is interface method`]" },
                            },
                        }

                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S011_InterfaceImpl.cs", new AdcConfig { Types = Array.Empty<AdcType>() });

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }

        [Test]
        public async Task S012_returns_valid_tree()
        {
            // Arrange
            var adcConfig = new AdcConfig
            {
                Types = new AdcType[1]
                {
                    new AdcType
                    {
                        TypeFullName = "RoslynScribe.OtherTestProject.IHandler",
                        Methods = new AdcMethod[1]
                        {
                            new AdcMethod { MethodName = "Handle", Level = 2 }
                        }
                    }
                }
            };

            var expected = new ScribeNode
            {
                Value = null,
                ChildNodes = new List<ScribeNode>()
                {
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S012 This is class`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S012 This is class method`]" },
                                ChildNodes = new List<ScribeNode>()
                                {
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[C:`RoslynScribe.OtherTestProject.IHandler.Handle`,L:`2`]" },
                                    }
                                }
                            },
                        }
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S012_AdcConfiguration.cs", adcConfig);

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }
    }
}
