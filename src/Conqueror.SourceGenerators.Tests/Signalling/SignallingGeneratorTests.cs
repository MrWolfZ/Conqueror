using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Conqueror.SourceGenerators.Signalling;
using VerifyNUnit;
using static Conqueror.SourceGenerators.Tests.TestSources;

namespace Conqueror.SourceGenerators.Tests.Signalling;

[TestFixture]
public sealed class SignallingGeneratorTests
{
    [Test]
    [TestCaseSource(nameof(GenerateSignalTypeTestCases))]
    public Task GivenCode_WhenRunningGenerator_GeneratesCorrectOutput(SourceGenerationTestCase testCase)
    {
        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new SignalTypeGenerator(), new SignalHandlerTypeGenerator()],
                                                                   [typeof(ISignalIdFactory).Assembly, typeof(JsonSerializerContext).Assembly],
                                                                   new(testCase.SourceCode));

        if (testCase.HasExpectedDiagnostics)
        {
            Assert.That(diagnostics, Is.Not.Empty, $"expected some diagnostics, but got none; output:\n{output}");
            return Verifier.Verify(output + "\n\nDiagnostics:\n" + string.Join("\n", diagnostics), CreateVerifySettings(testCase.Name));
        }

        Assert.That(diagnostics, Is.Empty, $"expected no diagnostics, but got some:\n{string.Join("\n", diagnostics)}\n\noutput:\n{output}");
        return Verifier.Verify(output, CreateVerifySettings(testCase.Name));
    }

    private static IEnumerable<TestCaseData> GenerateSignalTypeTestCases() => GenerateTestCases("Signalling");
}
