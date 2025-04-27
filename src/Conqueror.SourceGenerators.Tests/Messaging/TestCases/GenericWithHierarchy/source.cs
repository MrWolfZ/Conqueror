using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.GenericWithHierarchy;

[Message<TestMessageResponse>]
public abstract partial record TestMessage<TFirst, TSecond>;

[Message<TestMessageResponse>]
public partial record TestMessage : TestMessage<string, int>;

public record TestMessageResponse;

public partial class TestMessageHandler<TFirst, TSecond> : TestMessage<TFirst, TSecond>.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage<TFirst, TSecond> message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

public partial class TestMessageHandler2 : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage<TFirst, TSecond>
{
    public partial interface IHandler;
}

// make the compiler happy during design time
public partial record TestMessage
{
    public new partial interface IHandler;
}
