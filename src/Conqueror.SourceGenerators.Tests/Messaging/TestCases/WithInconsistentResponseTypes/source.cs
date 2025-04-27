using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Messaging;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithInconsistentResponseTypes;

[MessageTransport(Prefix = "Core", Namespace = "Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithInconsistentResponseTypes")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DuplicateMessageAttribute<TResponse> : Attribute;

// ExpectedDiagnostics
[Message<TestMessageResponse>]
[DuplicateMessage<TestMessageResponse2>]
public partial record TestMessage;

public record TestMessageResponse;

public record TestMessageResponse2;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}
