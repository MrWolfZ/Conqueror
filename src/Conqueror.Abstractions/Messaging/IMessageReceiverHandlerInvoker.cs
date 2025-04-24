using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageReceiverHandlerInvoker
{
    Task<TResponse> Invoke<TMessage, TResponse>(TMessage message,
                                                IServiceProvider serviceProvider,
                                                string transportTypeName,
                                                CancellationToken cancellationToken)
        where TMessage : class, IMessage<TMessage, TResponse>;
}

public interface IMessageReceiverHandlerInvoker<out TTypesInjector> : IMessageReceiverHandlerInvoker
    where TTypesInjector : class, IMessageHandlerTypesInjector
{
    Type MessageType { get; }

    Type ResponseType { get; }

    Type? HandlerType { get; }

    TTypesInjector TypesInjector { get; }
}
