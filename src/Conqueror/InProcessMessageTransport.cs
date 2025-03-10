using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror;

internal sealed class InProcessMessageTransport(Type handlerType, Delegate? configurePipeline) : IMessageTransportClient
{
    public const string Name = "in-process";

    public string TransportTypeName => Name;

    public Task<TResponse> Send<TMessage, TResponse>(TMessage message,
                                                     IServiceProvider serviceProvider,
                                                     CancellationToken cancellationToken)
        where TMessage : class, IMessage<TResponse>
    {
        var proxy = new MessageHandlerProxy<TMessage, TResponse>(serviceProvider,
                                                                 new(new HandlerInvoker(handlerType)),
                                                                 (Action<IMessagePipeline<TMessage, TResponse>>?)configurePipeline,
                                                                 MessageTransportRole.Server);

        return proxy.Handle(message, cancellationToken);
    }

    private sealed class HandlerInvoker(Type handlerType) : IMessageTransportClient
    {
        public string TransportTypeName => Name;

        public Task<TResponse> Send<TMessage, TResponse>(TMessage message,
                                                         IServiceProvider serviceProvider,
                                                         CancellationToken cancellationToken)
            where TMessage : class, IMessage<TResponse>
        {
            var handler = (IMessageHandler<TMessage, TResponse>)serviceProvider.GetRequiredService(handlerType);
            return handler.Handle(message, cancellationToken);
        }
    }
}
