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
        private readonly Dictionary<Type, Action<IEventObserverPipelineBuilder>> pipelineConfigurationMethodByObserverType;

        public EventMiddlewaresInvoker(IEnumerable<EventObserverPipelineConfiguration> observerPipelineConfigurations)
        {
            pipelineConfigurationMethodByObserverType = observerPipelineConfigurations.ToDictionary(c => c.ObserverType, c => c.Configure);
        }

        public async Task InvokeMiddlewares<TEvent>(IServiceProvider serviceProvider,
                                                    IReadOnlyCollection<EventObserverMetadata> metadataCol,
                                                    TEvent evt,
                                                    CancellationToken cancellationToken)
            where TEvent : class
        {
            var publisherMiddlewareInvokers = serviceProvider.GetRequiredService<IEnumerable<IEventPublisherMiddlewareInvoker>>().ToList();

            await ExecuteNextPublisherMiddleware(0, evt, cancellationToken).ConfigureAwait(false);

            async Task ExecuteNextPublisherMiddleware(int index, TEvent e, CancellationToken token)
            {
                if (index >= publisherMiddlewareInvokers.Count)
                {
                    await ExecuteEventObserverMiddlewares(serviceProvider, metadataCol, e, cancellationToken).ConfigureAwait(false);
                    return;
                }

                var publisherMiddlewareInvoker = publisherMiddlewareInvokers[index];
                await publisherMiddlewareInvoker.Invoke(e, (e2, t) => ExecuteNextPublisherMiddleware(index + 1, e2, t), serviceProvider, token).ConfigureAwait(false);
            }
        }

        private async Task ExecuteEventObserverMiddlewares<TEvent>(IServiceProvider serviceProvider,
                                                                   IReadOnlyCollection<EventObserverMetadata> metadataCol,
                                                                   TEvent evt,
                                                                   CancellationToken cancellationToken)
            where TEvent : class
        {
            // TODO: configurable strategy for order
            foreach (var metadata in metadataCol)
            {
                await ExecuteObserverMiddlewares(serviceProvider, metadata, evt, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ExecuteObserverMiddlewares<TEvent>(IServiceProvider serviceProvider,
                                                              EventObserverMetadata metadata,
                                                              TEvent evt,
                                                              CancellationToken cancellationToken)
            where TEvent : class
        {
            var pipelineBuilder = new EventObserverPipelineBuilder(serviceProvider);

            if (pipelineConfigurationMethodByObserverType.TryGetValue(metadata.ObserverType, out var configurationMethod))
            {
                configurationMethod(pipelineBuilder);
            }

            var pipeline = pipelineBuilder.Build();

            await pipeline.Execute(serviceProvider, metadata, evt, cancellationToken).ConfigureAwait(false);
        }
    }
}
