using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Conqueror.SourceGenerators.Messaging;
using VerifyNUnit;
using static Conqueror.SourceGenerators.Tests.TestSources;

namespace Conqueror.SourceGenerators.Tests.Messaging;

[TestFixture]
public sealed class MessagingGeneratorTests
{
    [Test]
    [TestCaseSource(nameof(GenerateMessageTypeTestCases))]
    public Task GivenCode_WhenRunningGenerator_GeneratesCorrectOutput(SourceGenerationTestCase testCase)
    {
        var (diagnostics, output) = TestHelpers.GetGeneratedOutput([new MessageTypeGenerator(), new MessageHandlerTypeGenerator()],
                                                                   [typeof(UnitMessageResponse).Assembly, typeof(JsonSerializerContext).Assembly],
                                                                   new(testCase.SourceCode));

        if (testCase.HasExpectedDiagnostics)
        {
            Assert.That(diagnostics, Is.Not.Empty, $"expected some diagnostics, but got none; output:\n{output}");
            return Verifier.Verify(output + "\n\nDiagnostics:\n" + string.Join("\n", diagnostics), CreateVerifySettings(testCase.Name));
        }

        Assert.That(diagnostics, Is.Empty, $"expected no diagnostics, but got some:\n{string.Join("\n", diagnostics)}\n\noutput:\n{output}");
        return Verifier.Verify(output, CreateVerifySettings(testCase.Name));
    }

    private static IEnumerable<TestCaseData> GenerateMessageTypeTestCases() => GenerateTestCases("Messaging");
}
