#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Messaging;

namespace Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransports;

[MessageTransport(Prefix = "TestTransport", Namespace = "Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransports")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TestTransportMessageAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

[MessageTransport(Prefix = "TestTransport", Namespace = "Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransports")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

[MessageTransport(Prefix = "TestTransport2", Namespace = "Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransports")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TestTransport2MessageAttribute : Attribute
{
    public string? StringProperty { get; init; }
}

[MessageTransport(Prefix = "TestTransport2", Namespace = "Conqueror.SourceGenerators.Tests.Messaging.TestCases.WithCustomTransports")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransport2MessageAttribute<TResponse> : TestTransport2MessageAttribute;

public interface ITestTransportMessage<out TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
{
    static virtual string StringProperty => "Default";
}

public interface ITestTransport2Message<out TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, ITestTransport2Message<TMessage, TResponse>
{
    static virtual string? StringProperty { get; }
}

public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
{
    static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotSupportedException();
}

public interface ITestTransport2MessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, ITestTransport2Message<TMessage, TResponse>
    where TIHandler : class, ITestTransport2MessageHandler<TMessage, TResponse, TIHandler>
{
    static IMessageHandlerTypesInjector CreateTestTransport2TypesInjector<THandler>()
        where THandler : class, TIHandler
        => throw new NotSupportedException();
}

[Message<TestMessageResponse>]
[TestTransportMessage<TestMessageResponse>(StringProperty = "Test")]
[TestTransport2Message<TestMessageResponse>(StringProperty = "Test2")]
public partial record TestMessage;

public record TestMessageResponse;

public partial class TestMessageHandler : TestMessage.IHandler
{
    public Task<TestMessageResponse> Handle(TestMessage message, CancellationToken cancellationToken) => throw new NotSupportedException();
}

// make the compiler happy during design time
public partial record TestMessage
{
    public partial interface IHandler;
}
