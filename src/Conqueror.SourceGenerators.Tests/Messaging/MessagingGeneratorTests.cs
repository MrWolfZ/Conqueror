using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Conqueror.SourceGenerators.Messaging;
using Microsoft.CodeAnalysis;
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

    [Test]
    public Task GivenHandlerInDifferentAssemblyThanMessageType_WhenRunningGenerator_GeneratesCorrectOutput()
    {
        var messageSource = """
                            using Conqueror;

                            namespace Generator.Tests.Messages;

                            [Message<TestMessageResponse>]
                            public sealed partial record TestMessage;
                            
                            public sealed partial record TestMessageResponse;
                            """;

        var handlerSource = """
                            using System;
                            using System.Threading;
                            using System.Threading.Tasks;
                            using Generator.Tests.Messages;

                            namespace Generator.Tests.Handlers;

                            public sealed partial class TestMessageHandler : TestMessage.IHandler
                            {
                                public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
                            }
                            """;

        var (diagnostics1, assembly) = TestHelpers.GetGeneratedAssembly("message",
                                                                        [new MessageTypeGenerator(), new MessageHandlerTypeGenerator()],
                                                                        [typeof(UnitMessageResponse).Assembly],
                                                                        new(messageSource));

        Assert.That(diagnostics1, Is.Empty, $"expected no diagnostics, but got some:\n{string.Join("\n", diagnostics1)}");
        Assert.That(assembly, Is.Not.Null, "expected an assembly to be generated");

        var (diagnostics2, output) = TestHelpers.GetGeneratedOutput([new MessageTypeGenerator(), new MessageHandlerTypeGenerator()],
                                                                    [typeof(UnitMessageResponse).Assembly],
                                                                    [MetadataReference.CreateFromImage(assembly)],
                                                                    new(handlerSource));

        Assert.That(diagnostics2, Is.Empty, $"expected no diagnostics, but got some:\n{string.Join("\n", diagnostics2)}\n\noutput:\n{output}");
        return Verifier.Verify(output, CreateVerifySettings("HandlerInDifferentAssembly"));
    }

    private static IEnumerable<TestCaseData> GenerateMessageTypeTestCases() => GenerateTestCases("Messaging");
}
