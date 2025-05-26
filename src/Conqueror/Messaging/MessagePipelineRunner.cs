using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessagePipelineRunner<TMessage, TResponse>(
    ConquerorContext conquerorContext,
    List<IMessageMiddleware<TMessage, TResponse>> middlewares)
    where TMessage : class, IMessage<TMessage, TResponse>
{
    public Task<TResponse> Execute(
        IServiceProvider serviceProvider,
        TMessage message,
        IMessageSender<TMessage, TResponse> sender,
        MessageTransportType transportType,
        CancellationToken cancellationToken)
    {
        if (middlewares.Count == 0)
        {
            return sender.Send(
                message,
                serviceProvider,
                conquerorContext,
                cancellationToken);
        }

        var ctx = new MessageMiddlewareContext<TMessage, TResponse>(middlewares, sender)
        {
            Message = message,
            TransportType = transportType,
            CancellationToken = cancellationToken,
            ConquerorContext = conquerorContext,
            ServiceProvider = serviceProvider,
        };

        return middlewares[0].Execute(ctx);
    }
}
