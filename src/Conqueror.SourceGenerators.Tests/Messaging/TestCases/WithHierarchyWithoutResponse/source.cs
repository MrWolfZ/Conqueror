using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithHierarchyWithoutResponse;

[Message]
public partial record TestMessage(int Payload);

[Message]
public partial record TestMessageSub(int Payload) : TestMessage(Payload);

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

public partial class TestMessageSubHandler : TestMessageSub.IHandler
{
    public Task Handle(TestMessageSub message, CancellationToken cancellationToken) => throw new NotSupportedException();
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
