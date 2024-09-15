using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal sealed class EventPublisherPipeline<TConfiguration>(IEnumerable<(Type MiddlewareType, object? MiddlewareConfiguration, IEventPublisherMiddlewareInvoker Invoker)> middlewares)
    where TConfiguration : Attribute, IConquerorEventTransportConfigurationAttribute
{
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IEventPublisherMiddlewareInvoker Invoker)> middlewares = middlewares.ToList();

    public async Task Execute<TEvent>(IServiceProvider serviceProvider,
                                      Type publisherType,
                                      TConfiguration configuration,
                                      TEvent initialEvent,
                                      CancellationToken cancellationToken)
        where TEvent : class
    {
        await ExecuteNextMiddleware(0, initialEvent, cancellationToken).ConfigureAwait(false);

        async Task ExecuteNextMiddleware(int index, TEvent evt, CancellationToken token)
        {
            if (index >= middlewares.Count)
            {
                var publisher = (IConquerorEventTransportPublisher<TConfiguration>)serviceProvider.GetRequiredService(publisherType);
                await publisher.PublishEvent(evt, configuration, serviceProvider, token).ConfigureAwait(false);
                return;
            }

            var (_, middlewareConfiguration, invoker) = middlewares[index];
            await invoker.Invoke(evt, (e, t) => ExecuteNextMiddleware(index + 1, e, t), middlewareConfiguration, serviceProvider, token).ConfigureAwait(false);
        }
    }
}
