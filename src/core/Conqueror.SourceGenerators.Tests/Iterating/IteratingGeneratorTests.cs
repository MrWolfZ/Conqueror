namespace Conqueror.SourceGenerators.Tests.Iterating;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SourceGenerators.Iterating;
using VerifyNUnit;
using static TestSources;

[TestFixture]
public sealed class IteratingGeneratorTests
{
    [Test]
    [TestCaseSource(nameof(GenerateIteratorTypeTestCases))]
    public Task GivenCode_WhenRunningGenerator_GeneratesCorrectOutput(SourceGenerationTestCase testCase)
    {
        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(
            [new IteratorTypeGenerator(), new IteratorHandlerTypeGenerator()],
            [typeof(UnitMessageResponse).Assembly, typeof(JsonSerializerContext).Assembly],
            new(testCase.SourceCode)
        );

        var nonHiddenDiagnostics = diagnostics.Where(d => d.Severity is not DiagnosticSeverity.Hidden).ToArray();

        if (testCase.HasExpectedDiagnostics)
        {
            Assert.That(
                nonHiddenDiagnostics,
                Is.Not.Empty,
                $"expected some diagnostics, but got none; output:\n{output}"
            );

            return Verifier.Verify(
                output + "\n\nDiagnostics:\n" + string.Join('\n', nonHiddenDiagnostics.AsEnumerable()),
                CreateVerifySettings(testCase.Name)
            );
        }

        Assert.That(
            nonHiddenDiagnostics,
            Is.Empty,
            $"expected no diagnostics, but got some:\n{string.Join('\n', nonHiddenDiagnostics.AsEnumerable())}\n\noutput:\n{output}"
        );

        return Verifier.Verify(output, CreateVerifySettings(testCase.Name));
    }

    [Test]
    public Task GivenHandlerInDifferentAssemblyThanIteratorType_WhenRunningGenerator_GeneratesCorrectOutput()
    {
        var iteratorSource = """
                             using Conqueror;

                             namespace Generator.Tests.Iterators;

                             [Iterator<TestItem>]
                             public sealed partial record TestIterator;

                             public sealed partial record TestItem;
                             """;

        var handlerSource = """
                            using System;
                            using System.Collections.Generic;
                            using System.Runtime.CompilerServices;
                            using System.Threading;
                            using System.Threading.Tasks;
                            using Generator.Tests.Iterators;

                            namespace Generator.Tests.Handlers;

                            public sealed partial class TestIteratorHandler : TestIterator.IHandler
                            {
                                public async IAsyncEnumerable<TestItem> Handle(TestIterator iterator, [EnumeratorCancellation] CancellationToken cancellationToken)
                                {
                                    await Task.CompletedTask;
                                    yield break;
                                }
                            }
                            """;

        var (diagnostics1, assembly) = TestHelpers.GetGeneratedAssembly(
            "iterator",
            [new IteratorTypeGenerator(), new IteratorHandlerTypeGenerator()],
            [typeof(UnitMessageResponse).Assembly],
            new(iteratorSource)
        );

        Assert.That(
            diagnostics1,
            Is.Empty,
            $"expected no diagnostics, but got some:\n{string.Join('\n', diagnostics1)}"
        );
        Assert.That(assembly, Is.Not.Null, "expected an assembly to be generated");

        var (diagnostics2, output) = TestHelpers.GetGeneratedOutput(
            [new IteratorTypeGenerator(), new IteratorHandlerTypeGenerator()],
            [typeof(UnitMessageResponse).Assembly],
            [MetadataReference.CreateFromImage(assembly)],
            new(handlerSource)
        );

        var nonHiddenDiagnostics2 = diagnostics2.Where(d => d.Severity is not DiagnosticSeverity.Hidden).ToArray();

        Assert.That(
            nonHiddenDiagnostics2,
            Is.Empty,
            $"expected no diagnostics, but got some:\n{string.Join('\n', nonHiddenDiagnostics2.AsEnumerable())}\n\noutput:\n{output}"
        );

        return Verifier.Verify(output, CreateVerifySettings("HandlerInDifferentAssembly"));
    }

    private static IEnumerable<TestCaseData> GenerateIteratorTypeTestCases() => GenerateTestCases("Iterating");
}
