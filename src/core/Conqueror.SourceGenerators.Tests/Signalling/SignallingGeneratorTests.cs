namespace Conqueror.SourceGenerators.Tests.Signalling;

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SourceGenerators.Signalling;
using VerifyNUnit;
using static TestSources;

[TestFixture]
public sealed class SignallingGeneratorTests
{
    [Test]
    [TestCaseSource(nameof(GenerateSignalTypeTestCases))]
    public Task GivenCode_WhenRunningGenerator_GeneratesCorrectOutput(SourceGenerationTestCase testCase)
    {
        var (diagnostics, output) = TestHelpers.GetGeneratedOutput(
            [new SignalTypeGenerator(), new SignalHandlerTypeGenerator()],
            [typeof(ISignalIdFactory).Assembly, typeof(JsonSerializerContext).Assembly],
            new(testCase.SourceCode)
        );

        if (testCase.HasExpectedDiagnostics)
        {
            Assert.That(diagnostics, Is.Not.Empty, $"expected some diagnostics, but got none; output:\n{output}");

            return Verifier.Verify(
                output + "\n\nDiagnostics:\n" + string.Join(separator: '\n', diagnostics),
                CreateVerifySettings(testCase.Name)
            );
        }

        Assert.That(
            diagnostics,
            Is.Empty,
            $"expected no diagnostics, but got some:\n{string.Join(separator: '\n', diagnostics)}\n\noutput:\n{output}"
        );

        return Verifier.Verify(output, CreateVerifySettings(testCase.Name));
    }

    [Test]
    public Task GivenHandlerInDifferentAssemblyThanSignalType_WhenRunningGenerator_GeneratesCorrectOutput()
    {
        var signalSource = """
                           using Conqueror;

                           namespace Generator.Tests.Signals;

                           [Signal]
                           public sealed partial record TestSignal;
                           """;

        var handlerSource = """
                            using System;
                            using System.Threading;
                            using System.Threading.Tasks;
                            using Generator.Tests.Signals;

                            namespace Generator.Tests.Handlers;

                            public sealed partial class TestSignalHandler : TestSignal.IHandler
                            {
                                public Task Handle(TestSignal signal, CancellationToken cancellationToken) => throw new NotSupportedException();
                            }
                            """;

        var (diagnostics1, assembly) = TestHelpers.GetGeneratedAssembly(
            "signal",
            [new SignalTypeGenerator(), new SignalHandlerTypeGenerator()],
            [typeof(ISignalIdFactory).Assembly],
            new(signalSource)
        );

        Assert.That(
            diagnostics1,
            Is.Empty,
            $"expected no diagnostics, but got some:\n{string.Join(separator: '\n', diagnostics1)}"
        );
        Assert.That(assembly, Is.Not.Null, "expected an assembly to be generated");

        var (diagnostics2, output) = TestHelpers.GetGeneratedOutput(
            [new SignalTypeGenerator(), new SignalHandlerTypeGenerator()],
            [typeof(ISignalIdFactory).Assembly],
            [MetadataReference.CreateFromImage(assembly)],
            new(handlerSource)
        );

        Assert.That(
            diagnostics2,
            Is.Empty,
            $"expected no diagnostics, but got some:\n{string.Join(separator: '\n', diagnostics2)}\n\noutput:\n{output}"
        );

        return Verifier.Verify(output, CreateVerifySettings("HandlerInDifferentAssembly"));
    }

    private static IEnumerable<TestCaseData> GenerateSignalTypeTestCases() => GenerateTestCases("Signalling");
}
