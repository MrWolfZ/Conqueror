using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Eventing.Observing;

namespace Conqueror.Eventing;

internal sealed class EventPipelineRunner<TEvent, TObservedEvent>(
    ConquerorContext conquerorContext,
    List<IEventMiddleware<TObservedEvent>> middlewares)
    where TEvent : class, TObservedEvent
    where TObservedEvent : class
{
    private readonly List<IEventMiddleware<TObservedEvent>> middlewares = middlewares.AsEnumerable().Reverse().ToList();

    public Task Execute(IServiceProvider serviceProvider,
                        Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                        TEvent initialEvent,
                        EventTransportType transportType,
                        CancellationToken cancellationToken)
    {
        var next = (TEvent evt, CancellationToken token) => observerFn(evt, serviceProvider, token);

        foreach (var middleware in middlewares)
        {
            var nextToCall = next;
            next = (evt, token) =>
            {
                var ctx = new DefaultEventMiddlewareContext<TObservedEvent>(evt,
                                                                            (q, t) => nextToCall((TEvent)q, t),
                                                                            serviceProvider,
                                                                            conquerorContext,
                                                                            transportType,
                                                                            token);

                return middleware.Execute(ctx);
            };
        }

        return next(initialEvent, cancellationToken);
    }
}
