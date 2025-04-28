using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithGenericResponse;

[Message<TestMessageResponse<Payload, int, Payload2>>]
public partial record TestMessage;

public record TestMessageResponse<T1, T2, T3>;

public record Payload;

public record Payload2;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse<Payload, int, Payload2>> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}
