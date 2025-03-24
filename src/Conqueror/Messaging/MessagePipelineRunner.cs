using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Messaging;

internal sealed class MessagePipelineRunner<TMessage, TResponse>(
    ConquerorContext conquerorContext,
    List<IMessageMiddleware<TMessage, TResponse>> middlewares)
    where TMessage : class, IMessage<TResponse>
{
    private readonly List<IMessageMiddleware<TMessage, TResponse>> middlewares = middlewares.AsEnumerable().Reverse().ToList();

    public async Task<TResponse> Execute(IServiceProvider serviceProvider,
                                         TMessage initialMessage,
                                         IMessageTransportClient<TMessage, TResponse> transportClient,
                                         MessageTransportType transportType,
                                         CancellationToken cancellationToken)
    {
        var next = (TMessage message, CancellationToken token) => transportClient.Send(message, serviceProvider, conquerorContext, token);

        foreach (var middleware in middlewares)
        {
            var nextToCall = next;
            next = (message, token) =>
            {
                var ctx = new DefaultMessageMiddlewareContext<TMessage, TResponse>(message,
                                                                                   (c, t) => nextToCall(c, t),
                                                                                   serviceProvider,
                                                                                   conquerorContext,
                                                                                   transportType,
                                                                                   token);

                return middleware.Execute(ctx);
            };
        }

        return await next(initialMessage, cancellationToken).ConfigureAwait(false);
    }
}
