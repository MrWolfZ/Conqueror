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
            evt = await ExecutePublisherMiddlewares(serviceProvider, evt, cancellationToken);

            // TODO: configurable strategy for order
            foreach (var (observer, metadata) in eventObservers)
            {
                await ExecuteObserverMiddlewares(serviceProvider, observer, metadata, evt, cancellationToken);
            }
        }

        private static async Task<TEvent> ExecutePublisherMiddlewares<TEvent>(IServiceProvider serviceProvider, TEvent evt, CancellationToken cancellationToken)
            where TEvent : class
        {
            var index = 0;
            var invokers = serviceProvider.GetRequiredService<IEnumerable<IEventPublisherMiddlewareInvoker>>().ToList();
            var result = evt;

            await ExecuteNextMiddleware(evt, cancellationToken);
            return result;

            async Task ExecuteNextMiddleware(TEvent e, CancellationToken token)
            {
                if (index >= invokers.Count)
                {
                    result = e;
                    return;
                }

                var publisherMiddlewareInvoker = invokers[index++];
                await publisherMiddlewareInvoker.Invoke(e, ExecuteNextMiddleware, serviceProvider, token);
            }
        }

        private static async Task ExecuteObserverMiddlewares<TEvent>(IServiceProvider serviceProvider,
                                                                     IEventObserver<TEvent> observer,
                                                                     EventObserverMetadata metadata,
                                                                     TEvent evt,
                                                                     CancellationToken cancellationToken)
            where TEvent : class
        {
            var index = 0;
            var attributes = metadata.MiddlewareConfigurationAttributes.ToList();

            await ExecuteNextMiddleware(evt, cancellationToken);

            async Task ExecuteNextMiddleware(TEvent e, CancellationToken token)
            {
                if (index >= attributes.Count)
                {
                    await observer.HandleEvent(e, token);
                    return;
                }
                
                var attribute = attributes[index++];
                var invoker = (IEventObserverMiddlewareInvoker)serviceProvider.GetService(typeof(EventObserverMiddlewareInvoker<>).MakeGenericType(attribute.GetType()))!;

                await invoker.Invoke(e, ExecuteNextMiddleware, metadata, attribute, serviceProvider, token);
            }
        }
    }
}
