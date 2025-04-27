using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.MultipleMessageTypesWithoutResponse;

[Message]
public partial record TestMessage;

[Message]
public partial record TestMessage2;

public partial class TestMessageHandler : TestMessage.IHandler,
                                          TestMessage2.IHandler
{
    public Task Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task Handle(TestMessage2 message, CancellationToken cancellationToken) => throw new NotSupportedException();
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
