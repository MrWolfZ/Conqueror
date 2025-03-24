using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Messaging;

internal sealed class InProcessMessageTransport<TMessage, TResponse>(
    Type handlerType,
    Delegate? configurePipeline,
    string? transportTypeName)
    : IMessageTransportClient<TMessage, TResponse>
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public string TransportTypeName => transportTypeName ?? InProcessMessageTransport.Name;

    public Task<TResponse> Send(TMessage message,
                                IServiceProvider serviceProvider,
                                ConquerorContext conquerorContext,
                                CancellationToken cancellationToken)
    {
        var proxy = new MessageHandlerProxy<TMessage, TResponse>(serviceProvider,
                                                                 new(new HandlerInvoker(handlerType, TransportTypeName)),
                                                                 (Action<IMessagePipeline<TMessage, TResponse>>?)configurePipeline,
                                                                 MessageTransportRole.Server);

        return proxy.Handle(message, cancellationToken);
    }

    private sealed class HandlerInvoker(Type handlerType, string transportTypeName) : IMessageTransportClient<TMessage, TResponse>
    {
        public string TransportTypeName => transportTypeName;

        public async Task<TResponse> Send(TMessage message,
                                          IServiceProvider serviceProvider,
                                          ConquerorContext conquerorContext,
                                          CancellationToken cancellationToken)
        {
            var handler = serviceProvider.GetRequiredService(handlerType);
            return await ((IMessageHandler<TMessage, TResponse>)handler).Handle(message, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal sealed class InProcessMessageTransport
{
    public const string Name = "in-process";
}
