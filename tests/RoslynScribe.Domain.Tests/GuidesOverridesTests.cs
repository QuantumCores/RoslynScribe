using NUnit.Framework;
using RoslynScribe.Domain.Extensions;
using RoslynScribe.Domain.Models;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Tests
{
    internal class GuidesOverridesTests
    {
        [Test]
        public void GuideOverride_overrides_original_UserDefinedId_property()
        {
            // Arrange
            var overrides = new Dictionary<string, string>
            {
                { ScribeGuidesTokens.UserDefinedId, $"{{{nameof(MethodInfo.ContainingTypeGenericParameters)}}}_handler" },
            };

            var guides = new ScribeGuides();
            var typeParameter = "RoslynScribe.TestProject.NugetMessage";
            var info = new MethodInfo
            {
                ContainingTypeGenericParameters = new[] { typeParameter }
            };

            // Act
            GuidesOverridesParser.Apply(overrides, guides, info);

            // Assert
            Assert.AreEqual(typeParameter + "_handler", guides.UserDefinedId);
        }

        [Test]
        public void GuideOverride_overrides_original_DestinationUserIds_property()
        {
            // Arrange
            var overrides = new Dictionary<string, string>
            {
                { ScribeGuidesTokens.DestinationUserIds, $"{{{nameof(MethodInfo.ParametersTypes)}}}_handler" },
            };

            var guides = new ScribeGuides();
            var parameterType = "RoslynScribe.TestProject.NugetMessage";
            var info = new MethodInfo
            {
                ParametersTypes = new[] { parameterType }
            };

            // Act
            GuidesOverridesParser.Apply(overrides, guides, info);

            // Assert
            Assert.AreEqual(parameterType + "_handler", guides.DestinationUserIds[0]);
        }
    }
}
