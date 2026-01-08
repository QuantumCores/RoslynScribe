using NUnit.Framework;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Extensions;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.Tests
{
    internal class GuidesOverridesTests
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
        public void GuideOverride_overrides_original_UserDefinedId_property()
        {
            // Arrange
            var overrides = new Dictionary<string, string>
            {
                { ScribeGuidesTokens.UserDefinedId, $"{{{nameof(MethodContext.ContainingTypeGenericParameters)}}}_handler" },
            };

            var guides = new ScribeGuides();
            var typeParameter = "RoslynScribe.TestProject.NugetMessage";
            var context = new MethodContext
            {
                ContainingTypeGenericParameters = new[] { typeParameter }
            };

            // Act
            GuidesOverridesParser.Apply(overrides, guides, context);

            // Assert
            Assert.AreEqual(typeParameter + "_handler", guides.UserDefinedId);
        }

        [Test]
        public void GuideOverride_overrides_original_DestinationUserIds_property()
        {
            // Arrange
            var overrides = new Dictionary<string, string>
            {
                { ScribeGuidesTokens.DestinationUserIds, $"{{{nameof(MethodContext.MethodParametersTypes)}}}_handler" },
            };

            var guides = new ScribeGuides();
            var parameterType = "RoslynScribe.TestProject.NugetMessage";
            var context = new MethodContext
            {
                MethodParametersTypes = new[] { parameterType }
            };

            // Act
            GuidesOverridesParser.Apply(overrides, guides, context);

            // Assert
            Assert.AreEqual(parameterType + "_handler", guides.DestinationUserIds[0]);
        }

        [Test]
        public void GuideOverride_gets_value_from_MethodAttributes_dictionary()
        {
            // Arrange
            var attributeName = "route";
            var attributeValue = "substitution";
            var overrides = new Dictionary<string, string>
            {
                { ScribeGuidesTokens.UserDefinedId, $"{{{nameof(MethodContext.MethodAttributes)}[{attributeName}]}}_url" },
            };

            var guides = new ScribeGuides();
            var context = new MethodContext
            {
                MethodAttributes = new Dictionary<string, string>() { { attributeName, attributeValue } }
            };

            // Act
            GuidesOverridesParser.Apply(overrides, guides, context);

            // Assert
            Assert.AreEqual(attributeValue + "_url", guides.UserDefinedId);
        }

        [Test]
        public void GuideOverride_gets_value_from_ContainingTypeAttributes_dictionary()
        {
            // Arrange
            var attributeName = "route";
            var attributeValue = "substitution";
            var overrides = new Dictionary<string, string>
            {
                { ScribeGuidesTokens.UserDefinedId, $"{{{nameof(MethodContext.ContainingTypeAttributes)}[{attributeName}]}}_url" },
            };

            var guides = new ScribeGuides();
            var context = new MethodContext
            {
                ContainingTypeAttributes = new Dictionary<string, string>() { { attributeName, attributeValue } }
            };

            // Act
            GuidesOverridesParser.Apply(overrides, guides, context);

            // Assert
            Assert.AreEqual(attributeValue + "_url", guides.UserDefinedId);
        }

        [Test]
        public async Task GuideOverride_applies_adc_config_attribute_replacements_to_UserDefinedId()
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
                            GetAttributes = new HashSet<string> { "Route" },
                            GetMethods = new AdcMethod[]
                            {
                                new AdcMethod { 
                                    MethodName = "LoadSomething", 
                                    SetDefaultLevel = 2, 
                                    IncludeMethodSignatures = true,
                                    GetAttributes = new HashSet<string> { "HttpPost" },
                                    SetGuidesOverrides = new Dictionary<string, string>
                                    {
                                        { ScribeGuidesTokens.UserDefinedId, $"{{{nameof(MethodContext.ContainingTypeAttributes)}[Route]}}/{{{nameof(MethodContext.MethodAttributes)}[HttpPost]}}" },
                                    }
                                },
                            },
                        }
                    }
                }
            };

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S017_Adc_NugetBaseClass.cs", adcConfig);

            // var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.AreEqual("api/v1/[controller]/Load", result.ChildNodes[0].Guides.UserDefinedId);
            Assert.AreEqual("api/v1/[controller]/test/Load2", result.ChildNodes[1].Guides.UserDefinedId);
        }

        [Test]
        public async Task GuideOverride_applies_adc_config_method_argument_replacements_to_UserDefinedId()
        {
            // Arrange
            var adcConfig = new AdcConfig
            {
                Types =
                {
                    {
                        "RoslynScribe.NugetTestProject.Senders.INugetSender",
                        new AdcType
                        {
                            SetGuidesOverrides = new Dictionary<string, string>
                            {
                                { ScribeGuidesTokens.UserDefinedId, $"{{{nameof(MethodContext.MethodArgumentsTypes)}[0]}}_handler" },
                            }
                        }
                    }
                }
            };
            adcConfig.FlattenMethodOverrides();

            // Act
            var result = await ScribeAnalyzer.Analyze(TestFixture.GetSolution(), "RoslynScribe.TestProject", "S018_Adc_NugetMethodWithObjectParam.cs", adcConfig);

            // var json = JsonSerializer.Serialize(result);
            // Assert
            Assert.AreEqual("RoslynScribe.TestProject.NugetMessage_handler", result.ChildNodes[0].Guides.UserDefinedId);
        }

    }
}
