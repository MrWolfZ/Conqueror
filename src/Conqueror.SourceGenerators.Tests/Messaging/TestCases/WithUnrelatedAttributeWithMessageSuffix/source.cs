using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithUnrelatedAttributeWithMessageSuffix;

[AttributeUsage(AttributeTargets.Class)]
public sealed class MyMessageAttribute : Attribute;

[MyMessage]
public partial record TestMessage
{
    public partial interface IHandler;
}

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}
