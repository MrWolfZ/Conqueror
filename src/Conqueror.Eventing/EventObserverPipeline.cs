using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal sealed class EventObserverPipeline
{
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares;

    public EventObserverPipeline(IEnumerable<(Type MiddlewareType, object? MiddlewareConfiguration)> middlewares)
    {
        this.middlewares = middlewares.ToList();
    }

    public async Task Execute<TEvent>(IServiceProvider serviceProvider,
                                      EventObserverMetadata metadata,
                                      TEvent initialEvent,
                                      CancellationToken cancellationToken)
        where TEvent : class
    {
        await ExecuteNextMiddleware(0, initialEvent, cancellationToken).ConfigureAwait(false);

        async Task ExecuteNextMiddleware(int index, TEvent evt, CancellationToken token)
        {
            if (index >= middlewares.Count)
            {
                var observer = (IEventObserver<TEvent>)serviceProvider.GetRequiredService(metadata.ObserverType);

                await observer.HandleEvent(evt, token).ConfigureAwait(false);
                return;
            }

            var (middlewareType, middlewareConfiguration) = middlewares[index];

            var invokerType = middlewareConfiguration is null
                ? typeof(EventObserverMiddlewareInvoker)
                : typeof(EventObserverMiddlewareInvoker<>).MakeGenericType(middlewareConfiguration.GetType());

            var invoker = (IEventObserverMiddlewareInvoker)Activator.CreateInstance(invokerType)!;

            await invoker.Invoke(evt, (e, t) => ExecuteNextMiddleware(index + 1, e, t), middlewareType, middlewareConfiguration, serviceProvider, token).ConfigureAwait(false);
        }
    }
}
