using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventNotificationPipelineRunner<TEventNotification>(
    ConquerorContext conquerorContext,
    List<IEventNotificationMiddleware<TEventNotification>> middlewares)
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    private readonly List<IEventNotificationMiddleware<TEventNotification>> middlewares = middlewares.AsEnumerable().Reverse().ToList();

    public async Task Execute(IServiceProvider serviceProvider,
                              TEventNotification initialEventNotification,
                              IEventNotificationPublisher<TEventNotification> publisher,
                              EventNotificationTransportType transportType,
                              CancellationToken cancellationToken)
    {
        var next = (TEventNotification notification, CancellationToken token) => publisher.Publish(notification, serviceProvider, conquerorContext, token);

        foreach (var middleware in middlewares)
        {
            var nextToCall = next;
            next = (notification, token) =>
            {
                var ctx = new DefaultEventNotificationMiddlewareContext<TEventNotification>(notification,
                                                                                            (c, t) => nextToCall(c, t),
                                                                                            serviceProvider,
                                                                                            conquerorContext,
                                                                                            transportType,
                                                                                            token);

                return middleware.Execute(ctx);
            };
        }

        await next(initialEventNotification, cancellationToken).ConfigureAwait(false);
    }
}
