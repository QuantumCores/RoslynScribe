using NUnit.Framework;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using RoslynScribe.Domain.Extensions;

namespace RoslynScribe.Domain.Tests
{
    public class ScribeAnalyzerTests
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            await TestFixture.Prepare();
        }

        [Test]
        public async Task Test1()
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
                                            }
                                        },
                                    }

                                },
                            }
                        },
                    },
                }
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S001_BasicComments.cs");

            // Assert
            Assert.IsTrue(result.IsEquivalent(expected));
        }
    }
}
