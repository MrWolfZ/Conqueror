using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithHierarchy;

[Message<TestMessageResponse>]
public partial record TestMessage(int Payload);

[Message<TestMessageResponse>]
public partial record TestMessageSub(int Payload) : TestMessage(Payload);

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

public partial class TestMessageSubHandler : TestMessageSub.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessageSub message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}

public partial record TestMessageSub
{
    public new partial interface IHandler;
}
