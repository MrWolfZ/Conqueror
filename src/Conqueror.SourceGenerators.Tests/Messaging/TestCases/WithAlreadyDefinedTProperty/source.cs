using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithAlreadyDefinedTProperty;

[Message<TestMessageResponse>]
public partial record TestMessage
{
    public string T { get; init; } = "test";

    public partial interface IHandler;
}

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}
