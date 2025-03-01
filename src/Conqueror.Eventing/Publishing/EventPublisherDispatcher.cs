using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Publishing;

internal sealed class EventPublisherDispatcher(EventPublisherRegistry publisherRegistry)
{
    private readonly ConcurrentDictionary<Type, IGenericDispatcher> dispatchersByPublisherType = new();

    public async Task DispatchEvent<TEvent>(TEvent evt, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var relevantPublishers = publisherRegistry.GetRelevantPublishersForEventType<TEvent>();

        var potentialExceptions = await Task.WhenAll(relevantPublishers.Select(p => DispatchEvent(evt, serviceProvider, p.Registration, p.Configuration, cancellationToken)))
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
                                                         IServiceProvider serviceProvider,
                                                         EventPublisherRegistration registration,
                                                         object configuration,
                                                         CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            var genericDispatcher = dispatchersByPublisherType.GetOrAdd(registration.PublisherType, _ => CreateDispatcher(registration));

            await genericDispatcher.DispatchEvent(evt, serviceProvider, registration, configuration, cancellationToken).ConfigureAwait(false);

            return null;
        }
        catch (Exception e)
        {
            return e;
        }
    }

    private static IGenericDispatcher CreateDispatcher(EventPublisherRegistration publisherRegistration)
    {
        return (IGenericDispatcher)Activator.CreateInstance(typeof(GenericDispatcher<>).MakeGenericType(publisherRegistration.ConfigurationAttributeType))!;
    }

    private interface IGenericDispatcher
    {
        Task DispatchEvent<TEvent>(TEvent evt,
                                   IServiceProvider serviceProvider,
                                   EventPublisherRegistration publisherRegistration,
                                   object configuration,
                                   CancellationToken cancellationToken)
            where TEvent : class;
    }

    private sealed class GenericDispatcher<TConfiguration> : IGenericDispatcher
        where TConfiguration : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        public Task DispatchEvent<TEvent>(TEvent evt,
                                          IServiceProvider serviceProvider,
                                          EventPublisherRegistration publisherRegistration,
                                          object configuration,
                                          CancellationToken cancellationToken)
            where TEvent : class
        {
            var publisherProxy = new EventPublisherProxy<TConfiguration>(publisherRegistration.PublisherType, publisherRegistration.ConfigurePipeline);

            return publisherProxy.PublishEvent(evt, (TConfiguration)configuration, serviceProvider, cancellationToken);
        }
    }
}
