using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing.Publishing;

internal sealed class EventPublisherDispatcher(EventPublisherRegistry publisherRegistry)
{
    private readonly ConcurrentDictionary<EventTransportPublisherRegistration, IGenericDispatcher> dispatchersByRegistration = new();

    public async Task DispatchEvent<TEvent>(TEvent evt,
                                            Action<IEventPipeline<TEvent>>? configurePipeline,
                                            IServiceProvider serviceProvider,
                                            CancellationToken cancellationToken)
        where TEvent : class
    {
        var relevantPublishers = publisherRegistry.GetRelevantPublishersForEventType(evt.GetType());

        var potentialExceptions = await Task.WhenAll(relevantPublishers.Select(p => DispatchEvent(evt,
                                                                                                  configurePipeline,
                                                                                                  serviceProvider,
                                                                                                  p.Registration,
                                                                                                  p.Configuration,
                                                                                                  cancellationToken)))
                                            .ConfigureAwait(false);

        var thrownExceptions = potentialExceptions.OfType<Exception>().ToList();

        if (thrownExceptions.Count == 0)
        {
            return;
        }

        if (thrownExceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(thrownExceptions[0]).Throw();
        }

        throw new AggregateException(thrownExceptions);
    }

    private async Task<Exception?> DispatchEvent<TEvent>(TEvent evt,
                                                         Action<IEventPipeline<TEvent>>? configurePipeline,
                                                         IServiceProvider serviceProvider,
                                                         EventTransportPublisherRegistration registration,
                                                         EventTransportAttribute configuration,
                                                         CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            var genericDispatcher = dispatchersByRegistration.GetOrAdd(registration, CreateDispatcher);

            await genericDispatcher.DispatchEvent(evt, configurePipeline, serviceProvider, registration, configuration, cancellationToken).ConfigureAwait(false);

            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    private static IGenericDispatcher CreateDispatcher(EventTransportPublisherRegistration publisherRegistration)
    {
        return (IGenericDispatcher)Activator.CreateInstance(typeof(GenericDispatcher<>).MakeGenericType(publisherRegistration.ConfigurationAttributeType))!;
    }

    private interface IGenericDispatcher
    {
        Task DispatchEvent<TEvent>(TEvent evt,
                                   Action<IEventPipeline<TEvent>>? configurePipeline,
                                   IServiceProvider serviceProvider,
                                   EventTransportPublisherRegistration publisherRegistration,
                                   EventTransportAttribute configuration,
                                   CancellationToken cancellationToken)
            where TEvent : class;
    }

    private sealed class GenericDispatcher<TConfiguration> : IGenericDispatcher
        where TConfiguration : EventTransportAttribute
    {
        public Task DispatchEvent<TEvent>(TEvent evt,
                                          Action<IEventPipeline<TEvent>>? configurePipeline,
                                          IServiceProvider serviceProvider,
                                          EventTransportPublisherRegistration publisherRegistration,
                                          EventTransportAttribute configuration,
                                          CancellationToken cancellationToken)
            where TEvent : class
        {
            return EventPipelineInvoker.RunPipeline(evt,
                                                    configurePipeline,
                                                    (TConfiguration)configuration,
                                                    serviceProvider,
                                                    EventTransportRole.Publisher,
                                                    (e, p, ct) =>
                                                    {
                                                        var publisher = (IEventTransportPublisher<TConfiguration>)p.GetRequiredService(publisherRegistration.PublisherType);
                                                        return publisher.PublishEvent(e, (TConfiguration)configuration, p, ct);
                                                    },
                                                    cancellationToken);
        }
    }
}
