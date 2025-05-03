using Conqueror.Messaging;

namespace Conqueror.Middleware.Logging.Tests.Messaging;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name",
                 Justification = "we want to bundle all files for the transport here, so the file name makes sense")]
[SuppressMessage("Performance", "CA1813:Avoid unsealed attributes", Justification = "by design")]
[MessageTransport(Prefix = "TestTransport", Namespace = "Conqueror.Middleware.Logging.Tests.Messaging")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TestTransportMessageAttribute : Attribute;

[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "used by source generator")]
[MessageTransport(Prefix = "TestTransport", Namespace = "Conqueror.Middleware.Logging.Tests.Messaging")]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TestTransportMessageAttribute<TResponse> : TestTransportMessageAttribute;

public interface ITestTransportMessage<out TMessage, TResponse> : IMessage<TMessage, TResponse>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>;

public interface ITestTransportMessageHandler<TMessage, TResponse, TIHandler> : IMessageHandler<TMessage, TResponse, TIHandler>
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
{
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "by design")]
    static IMessageHandlerTypesInjector CreateTestTransportTypesInjector<THandler>()
        where THandler : class, TIHandler
        => TestTransportMessageHandlerTypesInjector<TMessage, TResponse, TIHandler, THandler>.Default;
}

internal interface ITestTransportMessageHandlerTypesInjector : IMessageHandlerTypesInjector
{
    TResult Create<TResult>(ITestTransportMessageHandlerTypesInjectable<TResult> injectable);
}

file sealed class TestTransportMessageHandlerTypesInjector<TMessage, TResponse, TIHandler, THandler> : ITestTransportMessageHandlerTypesInjector
    where TMessage : class, ITestTransportMessage<TMessage, TResponse>
    where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
    where THandler : class, TIHandler
{
    public static readonly TestTransportMessageHandlerTypesInjector<TMessage, TResponse, TIHandler, THandler> Default = new();

    public Type MessageType { get; } = typeof(TMessage);

    public TResult Create<TResult>(ITestTransportMessageHandlerTypesInjectable<TResult> injectable)
        => injectable.WithInjectedTypes<TMessage, TResponse, TIHandler, THandler>();
}

public interface ITestTransportMessageHandlerTypesInjectable<out TResult>
{
    TResult WithInjectedTypes<TMessage, TResponse, TIHandler, THandler>()
        where TMessage : class, ITestTransportMessage<TMessage, TResponse>
        where TIHandler : class, ITestTransportMessageHandler<TMessage, TResponse, TIHandler>
        where THandler : class, TIHandler;
}
