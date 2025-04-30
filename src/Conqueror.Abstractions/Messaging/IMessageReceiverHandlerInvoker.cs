using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageReceiverHandlerInvoker
{
    Type MessageType { get; }

    Type ResponseType { get; }

    Type? HandlerType { get; }

    Task<TResponse> Invoke<TMessage, TResponse>(TMessage message,
                                                IServiceProvider serviceProvider,
                                                string transportTypeName,
                                                CancellationToken cancellationToken)
        where TMessage : class, IMessage<TMessage, TResponse>;
}

public interface IMessageReceiverHandlerInvoker<out TTypesInjector> : IMessageReceiverHandlerInvoker
    where TTypesInjector : class, IMessageHandlerTypesInjector
{
    TTypesInjector TypesInjector { get; }
}
