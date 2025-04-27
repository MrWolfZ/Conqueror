using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.Generic;

[Message<TestMessageResponse>]
public partial record TestMessage<TFirst, TSecond>;

public record TestMessageResponse;

public partial class TestMessageHandler<TFirst, TSecond> : TestMessage<TFirst, TSecond>.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage<TFirst, TSecond> message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

public partial class TestMessageHandler2 : TestMessage<string, int>.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage<string, int> message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage<TFirst, TSecond>
{
    public partial interface IHandler;
}
