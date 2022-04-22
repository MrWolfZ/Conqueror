using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing
{
    internal sealed class EventMiddlewaresInvoker
    {
        public async Task InvokeMiddlewares<TEvent>(IServiceProvider serviceProvider,
                                                    IReadOnlyCollection<(IEventObserver<TEvent> Observer, EventObserverMetadata Metadata)> eventObservers,
                                                    TEvent evt,
                                                    CancellationToken cancellationToken)
            where TEvent : class
        {
            var publisherMiddlewareInvokers = serviceProvider.GetRequiredService<IEnumerable<IEventPublisherMiddlewareInvoker>>().ToList();

            await ExecuteNextPublisherMiddleware(0, evt, cancellationToken);

            async Task ExecuteNextPublisherMiddleware(int index, TEvent e, CancellationToken token)
            {
                if (index >= publisherMiddlewareInvokers.Count)
                {
                    await ExecuteEventObserverMiddlewares(serviceProvider, eventObservers, e, cancellationToken);
                    return;
                }

                var publisherMiddlewareInvoker = publisherMiddlewareInvokers[index];
                await publisherMiddlewareInvoker.Invoke(e, (e2, t) => ExecuteNextPublisherMiddleware(index + 1, e2, t), serviceProvider, token);
            }
        }

        private static async Task ExecuteEventObserverMiddlewares<TEvent>(IServiceProvider serviceProvider,
                                                                          IReadOnlyCollection<(IEventObserver<TEvent> Observer, EventObserverMetadata Metadata)> eventObservers,
                                                                          TEvent evt,
                                                                          CancellationToken cancellationToken)
            where TEvent : class
        {
            // TODO: configurable strategy for order
            foreach (var (observer, metadata) in eventObservers)
            {
                await ExecuteObserverMiddlewares(serviceProvider, observer, metadata, evt, cancellationToken);
            }
        }

        private static async Task ExecuteObserverMiddlewares<TEvent>(IServiceProvider serviceProvider,
                                                                     IEventObserver<TEvent> observer,
                                                                     EventObserverMetadata metadata,
                                                                     TEvent evt,
                                                                     CancellationToken cancellationToken)
            where TEvent : class
        {
            var attributes = metadata.MiddlewareConfigurationAttributes.ToList();

            await ExecuteNextMiddleware(0, evt, cancellationToken);

            async Task ExecuteNextMiddleware(int index, TEvent e, CancellationToken token)
            {
                if (index >= attributes.Count)
                {
                    await observer.HandleEvent(e, token);
                    return;
                }

                var attribute = attributes[index];
                var invoker = (IEventObserverMiddlewareInvoker)serviceProvider.GetService(typeof(EventObserverMiddlewareInvoker<>).MakeGenericType(attribute.GetType()))!;

                await invoker.Invoke(e, (e2, t) => ExecuteNextMiddleware(index + 1, e2, t), metadata, attribute, serviceProvider, token);
            }
        }
    }
}
