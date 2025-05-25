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
        TMessage initialMessage,
        IMessageSender<TMessage, TResponse> sender,
        MessageTransportType transportType,
        CancellationToken cancellationToken)
    {
        if (middlewares.Count == 0)
        {
            return sender.Send(
                initialMessage,
                serviceProvider,
                conquerorContext,
                cancellationToken);
        }

        Task<TResponse> Send(TMessage message, CancellationToken token) => sender.Send(
            message,
            serviceProvider,
            conquerorContext,
            token);

        MessageMiddlewareNext<TMessage, TResponse> next = Send;

        for (var i = middlewares.Count - 1; i >= 0; i -= 1)
        {
            var middleware = middlewares[i];
            var nextToCall = next;
            next = Next;

            Task<TResponse> Next(TMessage message, CancellationToken token)
                => middleware.Execute(
                    new DefaultMessageMiddlewareContext<TMessage, TResponse>(
                        message,
                        nextToCall,
                        serviceProvider,
                        conquerorContext,
                        transportType,
                        token));
        }

        return next(initialMessage, cancellationToken);
    }
}
