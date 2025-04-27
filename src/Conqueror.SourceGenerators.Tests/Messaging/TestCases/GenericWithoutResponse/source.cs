using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.GenericWithoutResponse;

[Message]
public partial record TestMessage<TFirst, TSecond>;

public partial class TestMessageHandler<TFirst, TSecond> : TestMessage<TFirst, TSecond>.IHandler
{
    public Task Handle(TestMessage<TFirst, TSecond> message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

public partial class TestMessageHandler : TestMessage<string, int>.IHandler
{
    public Task Handle(TestMessage<string, int> message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage<TFirst, TSecond>
{
    public partial interface IHandler;
}
