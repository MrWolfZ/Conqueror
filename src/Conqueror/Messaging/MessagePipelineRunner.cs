using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessagePipelineRunner<TMessage, TResponse>(
    ConquerorContext conquerorContext,
    List<IMessageMiddleware<TMessage, TResponse>> middlewares)
    where TMessage : class, IMessage<TMessage, TResponse>
{
    private readonly List<IMessageMiddleware<TMessage, TResponse>> middlewares = middlewares.AsEnumerable().Reverse().ToList();

    public Task<TResponse> Execute(IServiceProvider serviceProvider,
                                   TMessage initialMessage,
                                   IMessageSender<TMessage, TResponse> sender,
                                   MessageTransportType transportType,
                                   CancellationToken cancellationToken)
    {
        MessageMiddlewareNext<TMessage, TResponse> next = (message, token) => sender.Send(message, serviceProvider, conquerorContext, token);

        foreach (var middleware in middlewares)
        {
            var nextToCall = next;
            next = (message, token) =>
            {
                var ctx = new DefaultMessageMiddlewareContext<TMessage, TResponse>(message,
                                                                                   nextToCall,
                                                                                   serviceProvider,
                                                                                   conquerorContext,
                                                                                   transportType,
                                                                                   token);

                return middleware.Execute(ctx);
            };
        }

        return next(initialMessage, cancellationToken);
    }
}
