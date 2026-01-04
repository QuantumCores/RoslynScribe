using NUnit.Framework;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Extensions;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S001_BasicComments.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S002_MultiLineComments.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S003_CommentFromLocalMethod.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S004_CommentFromOtherClassMethod.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S005_IfStatementBlockComment.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S006_ThirdPartyLibrary.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S007_CallLambda.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S008_CommentFromOtherProject.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S009_TwoMethodCalls.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S010_InfiniteRecursion.cs", new AdcConfig());

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
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S011_InterfaceImpl.cs", new AdcConfig());

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
                Types =
                {
                    { 
                        "RoslynScribe.OtherTestProject.IHandler",
                        new AdcType
                        {
                            // TypeFullName = "RoslynScribe.OtherTestProject.IHandler",
                            GetMethods = new AdcMethod[1]
                            {
                                new AdcMethod { MethodName = "Handle", SetDefaultLevel = 2, IncludeMethodDeclaration = true }
                            }
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S012 This is handler test class`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S012 These are handler invocations`]" },
                                ChildNodes = new List<ScribeNode>()
                                {
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.OtherTestProject.IHandler.Handle(object)`,L:`2`]" },
                                        ChildNodes = new List<ScribeNode>()
                                        {
                                            new ScribeNode
                                            {
                                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.OtherTestProject.IHandler.Handle(object)`,L:`2`]" },
                                            },
                                        },
                                    },
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.Handler.Handle(object)`,L:`2`]" },
                                        ChildNodes = new List<ScribeNode>()
                                        {
                                            new ScribeNode
                                            {
                                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.Handler.Handle(object)`,L:`2`]" },
                                            },
                                        },
                                    }
                                }
                            },
                        }
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.Handler.Handle(object)`,L:`2`]" },
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S012_Adc_SimpleInterface.cs", adcConfig);

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }

        [Test]
        public async Task S013_returns_valid_tree()
        {
            // Arrange
            var adcConfig = new AdcConfig
            {
                Types =
                {
                    {
                        "RoslynScribe.OtherTestProject.IHandler",
                        new AdcType
                        {
                            // TypeFullName = "RoslynScribe.OtherTestProject.IHandler",
                            GetMethods = new AdcMethod[1]
                            {
                                new AdcMethod { MethodName = "Handle", SetDefaultLevel = 2, IncludeMethodDeclaration = false }
                            }
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S013 This is handler test class`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S013 These are expanded handler invocations`]" },
                                ChildNodes = new List<ScribeNode>()
                                {
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.OtherTestProject.IHandler.Handle(object)`,L:`2`]" },
                                    },
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.S013_Adc_ExtendedInterface.ExpandedHandler.Handle(object)`,L:`2`]" },
                                    }
                                }
                            },
                        }
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S013_Adc_ExtendedInterface.cs", adcConfig);

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }

        [Test]
        public async Task S014_returns_valid_tree()
        {
            // Arrange
            var adcConfig = new AdcConfig
            {
                Types =
                {
                    {
                        "RoslynScribe.OtherTestProject.IExpandedHandler",
                        new AdcType
                        {
                            // TypeFullName = "RoslynScribe.OtherTestProject.IExpandedHandler",
                            GetMethods = new AdcMethod[1]
                            {
                                new AdcMethod { MethodName = "HandleWithResult", SetDefaultLevel = 2, IncludeMethodDeclaration = false }
                            }
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S014 This is handler test class`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S014 These are expanded handler with result invocations`]" },
                                ChildNodes = new List<ScribeNode>()
                                {
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.OtherTestProject.IExpandedHandler.HandleWithResult(object)`,L:`2`]" },
                                    },
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.S014_Adc_ExtendedInterfaceWithResult.ExpandedHandler.HandleWithResult(object)`,L:`2`]" },
                                    }
                                }
                            },
                        }
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S014_Adc_ExtendedInterfaceWithResult.cs", adcConfig);

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }

        [Test]
        public async Task S015_returns_valid_tree()
        {
            // Arrange
            var adcConfig = new AdcConfig
            {
                Types =
                {
                    {
                        "RoslynScribe.OtherTestProject.IGenericHandler<T123>",
                        new AdcType
                        {
                            // TypeFullName = "RoslynScribe.OtherTestProject.IGenericHandler<T123>",
                            GetMethods = new AdcMethod[1]
                            {
                                new AdcMethod { MethodName = "Handle", SetDefaultLevel = 2, IncludeMethodDeclaration = false }
                            }
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S015 This is handler test class`]" },
                        ChildNodes = new List<ScribeNode>()
                        {
                            new ScribeNode
                            {
                                Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S015 These are handler invocations`]" },
                                ChildNodes = new List<ScribeNode>()
                                {
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.OtherTestProject.IGenericHandler<RoslynScribe.TestProject.Message>.Handle(T123)`,L:`2`]" },
                                    },
                                    new ScribeNode
                                    {
                                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.GenericHandler<RoslynScribe.TestProject.Message>.Handle(T)`,L:`2`]" },
                                    }
                                }
                            },
                        }
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S015_Adc_GenericInterface.cs", adcConfig);

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }

        [Test]
        public async Task S016_returns_valid_tree()
        {
            // Arrange
            var adcConfig = new AdcConfig
            {
                Types =
                {
                    {
                        "RoslynScribe.NugetTestProject.Hanlders.INugetHandler<T>",
                        new AdcType
                        {
                            // TypeFullName = "RoslynScribe.NugetTestProject.Hanlders.INugetHandler<T>",
                            GetMethods = new AdcMethod[1]
                            {
                                new AdcMethod { MethodName = "Handle", SetDefaultLevel = 3, IncludeMethodDeclaration = true }
                            }
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`S016 This is nuget test class without invocations`]" },                        
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.NugetHandler<T>.Handle(T)`,L:`3`]" },
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.NugetMessageHandler.Handle(RoslynScribe.TestProject.NugetMessage)`,L:`3`]" },
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S016_Adc_NugetInterface.cs", adcConfig);

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }

        [Test]
        public async Task S017_returns_valid_tree()
        {
            // Arrange
            var adcConfig = new AdcConfig
            {
                Types =
                {
                    {
                        "Microsoft.AspNetCore.Mvc.ControllerBase",
                        new AdcType
                        {
                            GetMethods = new AdcMethod[]
                            {
                                new AdcMethod { MethodName = "LoadSomething", SetDefaultLevel = 2, IncludeMethodDeclaration = true }
                            },
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
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.S017_Adc_NugetBaseClass.LoadSomething(int, int)`,L:`2`]" },
                    },
                    new ScribeNode
                    {
                        Value = new string[] { $"// {ScribeAnalyzer.CommentLabel}[T:`RoslynScribe.TestProject.S017_Adc_NugetBaseClass.LoadSomething(int)`,L:`2`]" },
                    },
                },
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S017_Adc_NugetBaseClass.cs", adcConfig);

            // var json = JsonSerializer.Serialize(result);
            // Assert
            var isEquivalent = result.IsEquivalent(expected);
            Assert.IsTrue(isEquivalent.Result, isEquivalent.Text);
        }
    }
}
