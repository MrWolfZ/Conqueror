using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing.Observing;

internal sealed class EventObserverPipeline(IEnumerable<(Type MiddlewareType, object? MiddlewareConfiguration, IEventObserverMiddlewareInvoker Invoker)> middlewares)
{
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IEventObserverMiddlewareInvoker Invoker)> middlewares = middlewares.ToList();

    public async Task Execute<TEvent>(IServiceProvider serviceProvider,
                                      Type observerType,
                                      TEvent initialEvent,
                                      Type observedEventType,
                                      CancellationToken cancellationToken)
        where TEvent : class
    {
        await ExecuteNextMiddleware(0, initialEvent, cancellationToken).ConfigureAwait(false);

        async Task ExecuteNextMiddleware(int index, TEvent evt, CancellationToken token)
        {
            if (index >= middlewares.Count)
            {
                var observer = (IEventObserver<TEvent>)serviceProvider.GetRequiredService(observerType);
                await observer.HandleEvent(evt, token).ConfigureAwait(false);
                return;
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];
            await invoker.Invoke(evt, observedEventType, (e, t) => ExecuteNextMiddleware(index + 1, e, t), middlewareConfiguration, serviceProvider, token).ConfigureAwait(false);
        }
    }

    public async Task Execute<TEvent>(IServiceProvider serviceProvider,
                                      Func<TEvent, IServiceProvider, CancellationToken, Task> observerFn,
                                      TEvent initialEvent,
                                      Type observedEventType,
                                      CancellationToken cancellationToken)
        where TEvent : class
    {
        await ExecuteNextMiddleware(0, initialEvent, cancellationToken).ConfigureAwait(false);

        async Task ExecuteNextMiddleware(int index, TEvent evt, CancellationToken token)
        {
            if (index >= middlewares.Count)
            {
                await observerFn(evt, serviceProvider, token).ConfigureAwait(false);
                return;
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];
            await invoker.Invoke(evt, observedEventType, (e, t) => ExecuteNextMiddleware(index + 1, e, t), middlewareConfiguration, serviceProvider, token).ConfigureAwait(false);
        }
    }
}
