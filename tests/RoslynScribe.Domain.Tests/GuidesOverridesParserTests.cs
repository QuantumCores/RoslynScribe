using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using RoslynScribe.Domain.Extensions;
using RoslynScribe.Domain.Models;
using System.Collections.Generic;

namespace RoslynScribe.Domain.Tests
{
    public class GuidesOverridesParserTests
    {
        private static MethodContext CreateMethodInfo()
        {
            return new MethodContext
            {
                MethodName = "DoWork",
                ContainingType = "My.Namespace.MyType",
                MethodIdentifier = "DoWork(int x)",
                ContainingTypeGenericParameters = new[] { "T", "U" },
                MethodParametersTypes = new[] { "V", "W" }
            };
        }

        [Test]
        public void Parse_returns_empty_string_for_null_or_whitespace()
        {
            var info = CreateMethodInfo();

            Assert.That(GuidesOverridesParser.Parse(null, info), Is.EqualTo(string.Empty));
            Assert.That(GuidesOverridesParser.Parse("", info), Is.EqualTo(string.Empty));
            Assert.That(GuidesOverridesParser.Parse("   ", info), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Parse_returns_original_value_when_no_tokens()
        {
            var info = CreateMethodInfo();
            var value = "no tokens here";

            Assert.That(GuidesOverridesParser.Parse(value, info), Is.EqualTo(value));
        }

        [Test]
        public void Parse_replaces_single_token_and_keeps_prefix_and_suffix()
        {
            var info = CreateMethodInfo();

            var value = $"before {{{nameof(MethodContext.MethodName)}}} after";
            var parsed = GuidesOverridesParser.Parse(value, info);

            Assert.That(parsed, Is.EqualTo("before DoWork after"));
        }

        [Test]
        public void Parse_replaces_multiple_tokens()
        {
            var info = CreateMethodInfo();

            var value = $"{{{nameof(MethodContext.ContainingType)}}}.{{{nameof(MethodContext.MethodName)}}} -> {{{nameof(MethodContext.MethodIdentifier)}}} <{{{nameof(MethodContext.ContainingTypeGenericParameters)}}}>({{{nameof(MethodContext.MethodParametersTypes)}}})";
            var parsed = GuidesOverridesParser.Parse(value, info);

            Assert.That(parsed, Is.EqualTo("My.Namespace.MyType.DoWork -> DoWork(int x) <T|U>(V|W)"));
        }

        [Test]
        public void Parse_leaves_unknown_tokens_intact()
        {
            var info = CreateMethodInfo();

            var value = "prefix {DoesNotExist} suffix";
            var parsed = GuidesOverridesParser.Parse(value, info);

            Assert.That(parsed, Is.EqualTo(value));
        }

        [Test]
        public void Apply_returns_same_instance_when_overrides_null_or_empty()
        {
            var info = CreateMethodInfo();
            var guide = ScribeGuides.Default();

            var resultNull = GuidesOverridesParser.Apply(null, guide, info, SyntaxKind.InvocationExpression);
            var resultEmpty = GuidesOverridesParser.Apply(new Dictionary<string, string>(), guide, info, SyntaxKind.InvocationExpression);

            Assert.That(resultNull, Is.SameAs(guide));
            Assert.That(resultEmpty, Is.SameAs(guide));
        }

        [Test]
        public void Apply_sets_properties_using_token_keys_and_parses_placeholders()
        {
            var info = CreateMethodInfo();
            var guide = ScribeGuides.Default();

            var overrides = new Dictionary<string, string>
            {
                [ScribeGuidesTokens.Description] = $"desc {{{nameof(MethodContext.MethodName)}}}",
                [ScribeGuidesTokens.Text] = $"text {{{nameof(MethodContext.ContainingType)}}}",
                [ScribeGuidesTokens.UserDefinedId] = $"uid {{{nameof(MethodContext.MethodIdentifier)}}}",
                [ScribeGuidesTokens.Path] = $"/{{{nameof(MethodContext.ContainingType)}}}/{{{nameof(MethodContext.MethodName)}}}",
                [ScribeGuidesTokens.Tags] = "tag1;tag2",
                [ScribeGuidesTokens.OriginUserIds] = "origin1;origin2",
                [ScribeGuidesTokens.DestinationUserIds] = "dest1;dest2",
            };

            GuidesOverridesParser.Apply(overrides, guide, info, SyntaxKind.ExpressionElement);

            Assert.That(guide.Description, Is.EqualTo("desc DoWork"));
            Assert.That(guide.Text, Is.EqualTo("text My.Namespace.MyType"));
            Assert.That(guide.UserDefinedId, Is.EqualTo("uid DoWork(int x)"));
            Assert.That(guide.Path, Is.EqualTo("/My.Namespace.MyType/DoWork"));
            Assert.That(guide.Tags, Is.EquivalentTo(new[] { "tag1", "tag2" }));
            // Assert.That(guide.OriginUserIds, Is.EquivalentTo(new[] { "origin1", "origin2" }));
            Assert.That(guide.DestinationUserIds, Is.EquivalentTo(new[] { "dest1", "dest2" }));
        }

        [Test]
        public void Apply_accepts_property_name_keys_case_insensitively()
        {
            var info = CreateMethodInfo();
            var guide = ScribeGuides.Default();

            var overrides = new Dictionary<string, string>
            {
                ["description"] = $"A {{{nameof(MethodContext.MethodName)}}}",
                ["TEXT"] = $"B {{{nameof(MethodContext.ContainingType)}}}",
                ["UserDefinedId"] = $"C {{{nameof(MethodContext.MethodIdentifier)}}}",
                ["pAtH"] = $"D {{{nameof(MethodContext.MethodName)}}}",
                ["OriginUserIds"] = "o1;o2",
                ["DestinationUserIds"] = "d1;d2",
            };

            GuidesOverridesParser.Apply(overrides, guide, info, SyntaxKind.ExpressionElement);

            Assert.That(guide.Description, Is.EqualTo("A DoWork"));
            Assert.That(guide.Text, Is.EqualTo("B My.Namespace.MyType"));
            Assert.That(guide.UserDefinedId, Is.EqualTo("C DoWork(int x)"));
            Assert.That(guide.Path, Is.EqualTo("D DoWork"));
            // Assert.That(guide.OriginUserIds, Is.EquivalentTo(new[] { "o1", "o2" }));
            Assert.That(guide.DestinationUserIds, Is.EquivalentTo(new[] { "d1", "d2" }));
        }
    }
}


