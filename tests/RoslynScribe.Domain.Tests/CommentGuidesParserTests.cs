using NUnit.Framework;
using RoslynScribe.Domain.Extensions;

namespace RoslynScribe.Domain.Tests
{
    public class CommentGuidesParserTests
    {
        [Test]
        public void Parses_description_from_backtick_value()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                "// [ADC][D:`this is some comment`]"
            });

            Assert.That(parsed.Guide.Description, Is.EqualTo("this is some comment"));
        }

        [Test]
        public void Parses_test_from_T_key()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                "// [ADC][T:`Lorem ipsum`]"
            });

            Assert.That(parsed.Guide.Text, Is.EqualTo("Lorem ipsum"));
        }

        [Test]
        public void Parses_multiple_properties_and_splits_tags()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                "// [ADC][D:`this is some comment`, Tags:`processA; processB`]"
            });

            Assert.That(parsed.Guide.Description, Is.EqualTo("this is some comment"));
            Assert.That(parsed.Guide.Tags, Is.EquivalentTo(new[] { "processA", "processB" }));
        }

        [Test]
        public void Does_not_require_escaping_apostrophes()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                "// [ADC][D:`don't escape this`]"
            });

            Assert.That(parsed.Guide.Description, Is.EqualTo("don't escape this"));
        }

        [Test]
        public void Supports_escaped_backtick_in_value()
        {
            var parsed = ScribeCommnetParser.Parse(new[]
            {
                "// [ADC][D:`use \\`backticks\\` sometimes`]"
            });

            Assert.That(parsed.Guide.Description, Is.EqualTo("use `backticks` sometimes"));
        }
    }
}

