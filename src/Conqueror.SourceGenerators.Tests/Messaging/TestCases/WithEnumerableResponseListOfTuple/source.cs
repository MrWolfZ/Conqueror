using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithEnumerableResponseListOfTuple;

[Message<List<(TestMessageResponse, int, long[], Payload)>>]
public partial record TestMessage;

public record TestMessageResponse;

public record Payload;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<List<(TestMessageResponse, int, long[], Payload)>> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}
