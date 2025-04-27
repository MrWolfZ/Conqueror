using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.MultipleMessageTypes;

[Message<TestMessageResponse>]
public partial record TestMessage;

[Message<TestMessageResponse2>]
public partial record TestMessage2;

public record TestMessageResponse;

public record TestMessageResponse2;

public partial class TestMessageHandler : TestMessage.IHandler,
                                          TestMessage2.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<TestMessageResponse2> Handle(TestMessage2 message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}

public partial record TestMessage2
{
    public partial interface IHandler;
}
