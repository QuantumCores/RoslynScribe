using NUnit.Framework;
using RoslynScribe.Domain.Extensions;
using RoslynScribe.Domain.Models;

namespace RoslynScribe.Domain.Tests
{
    public class CommentGuidesParserTests
    {
        [Test]
        public void Parses_description_from_backtick_value()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                $"// [ADC][{ScribeGuidesTokens.Description}:`this is some comment`]"
            });

            Assert.That(parsed.Description, Is.EqualTo("this is some comment"));
        }

        [Test]
        public void Parses_test_from_T_key()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                $"// [ADC][{ScribeGuidesTokens.Text}:`Lorem ipsum`]"
            });

            Assert.That(parsed.Text, Is.EqualTo("Lorem ipsum"));
        }

        [Test]
        public void Parses_multiple_properties_and_splits_tags()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                $"// [ADC][{ScribeGuidesTokens.Description}:`this is some comment`, {ScribeGuidesTokens.Tags}:`processA; processB`, {ScribeGuidesTokens.Id}:`identifier`,"
                + $" {ScribeGuidesTokens.UserDefinedId}:`userIdentifier`, {ScribeGuidesTokens.Text}:`text`, {ScribeGuidesTokens.Path}:`path`, {ScribeGuidesTokens.Level}:`2`, "
                + $"{ScribeGuidesTokens.OriginUserIds}:`origin`, {ScribeGuidesTokens.DestinationUserIds}:`destination1; destination2`]"
            });

            Assert.That(parsed.Description, Is.EqualTo("this is some comment"));
            Assert.That(parsed.Tags, Is.EquivalentTo(new[] { "processA", "processB" }));
            Assert.That(parsed.Id, Is.EqualTo("identifier"));
            Assert.That(parsed.UserDefinedId, Is.EqualTo("userIdentifier"));
            Assert.That(parsed.Text, Is.EqualTo("text"));
            Assert.That(parsed.Path, Is.EqualTo("path"));
            Assert.That(parsed.Level, Is.EqualTo(2));
            Assert.That(parsed.OriginUserIds, Is.EquivalentTo(new[] { "origin" }));
            Assert.That(parsed.DestinationUserIds, Is.EquivalentTo(new[] { "destination1", "destination2" }));
            
        }

        [Test]
        public void Does_not_require_escaping_apostrophes()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                $"// [ADC][{ScribeGuidesTokens.Description}:`don't escape this`]"
            });

            Assert.That(parsed.Description, Is.EqualTo("don't escape this"));
        }

        [Test]
        public void Supports_escaped_backtick_in_value()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                $"// [ADC][{ScribeGuidesTokens.Description}:`use \\`backticks\\` sometimes`]"
            });

            Assert.That(parsed.Description, Is.EqualTo("use `backticks` sometimes"));
        }
    }
}

