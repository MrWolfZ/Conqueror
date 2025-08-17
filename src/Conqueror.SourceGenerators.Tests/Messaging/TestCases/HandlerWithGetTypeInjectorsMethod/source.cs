namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.HandlerWithGetTypeInjectorsMethod;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[Message<TestMessageResponse>]
public partial record TestMessage;

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    static IEnumerable<IMessageHandlerTypesInjector> IMessageHandler.GetTypeInjectors() =>
        throw new NotSupportedException();

    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}

public partial class TestMessageHandler : IMessageHandler;
