using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using RoslynScribe.Domain.Configuration;
using RoslynScribe.Domain.Models;
using RoslynScribe.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynScribe.Domain.Tests
{
    public class ImplementationResolverTests
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
        public async Task ResolveImplementationMethodSymbol_UsesConfiguredImplementation()
        {
            var (invocation, semanticModel, documents) = await GetInvocation(
                "S021_ImplementationResolver.cs",
                "S021_ImplementationResolver_Config",
                "Run");

            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            var adcConfig = new AdcConfig
            {
                InterfaceImplementations =
                {
                    {
                        "RoslynScribe.TestProject.IMultiResolverHandler",
                        "RoslynScribe.TestProject.MultiResolverHandlerB"
                    }
                }
            };

            var trackers = new Trackers { Solution = TestFixture.GetSolution() };

            var implementation = ImplementationResolver.ResolveImplementationMethodSymbol(
                methodSymbol,
                invocation,
                semanticModel,
                documents,
                adcConfig,
                trackers);

            Assert.IsNotNull(implementation);
            Assert.AreEqual("RoslynScribe.TestProject.MultiResolverHandlerB", implementation.ContainingType.ToDisplayString());
            Assert.AreEqual("Handle", implementation.Name);
        }

        [Test]
        public async Task ResolveImplementationMethodSymbol_ReturnsNull_WhenMultipleImplementationsAndNoConfig()
        {
            var (invocation, semanticModel, documents) = await GetInvocation(
                "S021_ImplementationResolver.cs",
                "S021_ImplementationResolver_Config",
                "Run");

            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            var trackers = new Trackers { Solution = TestFixture.GetSolution() };

            var implementation = ImplementationResolver.ResolveImplementationMethodSymbol(
                methodSymbol,
                invocation,
                semanticModel,
                documents,
                new AdcConfig(),
                trackers);

            Assert.IsNull(implementation);
        }

        [Test]
        public async Task ResolveImplementationMethodSymbol_ResolvesFromFactoryAssignment()
        {
            var (invocation, semanticModel, documents) = await GetInvocation(
                "S021_ImplementationResolver.cs",
                "S022_ImplementationResolver_Assignment",
                "Run");

            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            var trackers = new Trackers { Solution = TestFixture.GetSolution() };

            var implementation = ImplementationResolver.ResolveImplementationMethodSymbol(
                methodSymbol,
                invocation,
                semanticModel,
                documents,
                new AdcConfig(),
                trackers);

            Assert.IsNotNull(implementation);
            Assert.AreEqual("RoslynScribe.TestProject.MultiResolverHandlerA", implementation.ContainingType.ToDisplayString());
            Assert.AreEqual("Handle", implementation.Name);
        }

        [Test]
        public async Task ResolveImplementationMethodSymbol_UsesUniqueImplementationFallback()
        {
            var (invocation, semanticModel, documents) = await GetInvocation(
                "S021_ImplementationResolver.cs",
                "S023_ImplementationResolver_Unique",
                "Run");

            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            var trackers = new Trackers { Solution = TestFixture.GetSolution() };

            var implementation = ImplementationResolver.ResolveImplementationMethodSymbol(
                methodSymbol,
                invocation,
                semanticModel,
                documents,
                new AdcConfig(),
                trackers);

            Assert.IsNotNull(implementation);
            Assert.AreEqual("RoslynScribe.TestProject.UniqueResolverHandler", implementation.ContainingType.ToDisplayString());
            Assert.AreEqual("Handle", implementation.Name);
        }

        private static async Task<(InvocationExpressionSyntax Invocation, SemanticModel SemanticModel, Dictionary<string, Document> Documents)> GetInvocation(
            string documentName,
            string className,
            string methodName)
        {
            var solution = TestFixture.GetSolution();
            var project = solution.Projects.Single(p => string.Equals(p.Name, "RoslynScribe.TestProject", StringComparison.Ordinal));
            var document = project.Documents.Single(d => string.Equals(d.Name, documentName, StringComparison.Ordinal));
            var semanticModel = await document.GetSemanticModelAsync();
            var root = await document.GetSyntaxRootAsync();
            var classNode = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Single(c => string.Equals(c.Identifier.ValueText, className, StringComparison.Ordinal));
            var method = classNode.Members
                .OfType<MethodDeclarationSyntax>()
                .Single(m => string.Equals(m.Identifier.ValueText, methodName, StringComparison.Ordinal));
            var invocation = method.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

            var documents = solution.Projects
                .SelectMany(p => p.Documents)
                .Where(d => !string.IsNullOrWhiteSpace(d.FilePath))
                .ToDictionary(d => d.FilePath, StringComparer.OrdinalIgnoreCase);

            return (invocation, semanticModel, documents);
        }
    }
}
