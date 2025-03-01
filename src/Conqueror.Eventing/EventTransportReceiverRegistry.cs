using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Conqueror.Eventing.Observing;
using Conqueror.Eventing.Publishing;

namespace Conqueror.Eventing;

internal sealed class EventTransportReceiverRegistry(
    IServiceProvider serviceProvider,
    IEnumerable<EventObserverRegistration> registrations)
    : IConquerorEventTransportReceiverRegistry, IDisposable
{
    private readonly IReadOnlyCollection<EventObserverRegistration> registrations = registrations.ToList();
    private readonly SemaphoreSlim semaphore = new(1);

    private Dictionary<ConquerorEventObserverId, IReadOnlyCollection<IEventObserverTransportConfiguration>>? transportConfigurationsByObserverId;

    public async Task<ConquerorEventTransportReceiverRegistration<TObserverTransportConfiguration, TConfigurationAttribute>> RegisterReceiver<TObserverTransportConfiguration, TConfigurationAttribute>(
        Action<IConquerorEventBroadcastingStrategyBuilder> broadcastingStrategyConfiguration)
        where TObserverTransportConfiguration : class, IEventObserverTransportConfiguration
        where TConfigurationAttribute : Attribute, IConquerorEventTransportConfigurationAttribute
    {
        var configurationsByObserverId = await GetTransportConfigurations().ConfigureAwait(false);

        var relevantTransportClientObserversRegistrations = new List<ConquerorEventTransportReceiverObserverRegistration<TObserverTransportConfiguration, TConfigurationAttribute>>();
        var relevantObserverRegistrations = new List<EventObserverRegistration>();

        foreach (var (registration, eventType) in from r in registrations from et in r.ObservedEventTypes select (r, et))
        {
            var configurations = configurationsByObserverId.TryGetValue(registration.ObserverId, out var c) ? c : new List<IEventObserverTransportConfiguration>();
            var configuration = configurations.OfType<TObserverTransportConfiguration>().FirstOrDefault();

            var hasAnyConfigurationAttributes = eventType.GetCustomAttributes().OfType<IConquerorEventTransportConfigurationAttribute>().Any();
            var configurationAttribute = eventType.GetCustomAttribute<TConfigurationAttribute>();

            if (configurationAttribute is null)
            {
                if (hasAnyConfigurationAttributes || typeof(TConfigurationAttribute) != typeof(InMemoryEventAttribute))
                {
                    continue;
                }

                relevantObserverRegistrations.Add(registration);
                relevantTransportClientObserversRegistrations.Add(new(registration.ObserverId, eventType, (TConfigurationAttribute)(object)new InMemoryEventAttribute(), configuration));
                continue;
            }

            relevantObserverRegistrations.Add(registration);
            relevantTransportClientObserversRegistrations.Add(new(registration.ObserverId, eventType, configurationAttribute, configuration));
        }

        var strategyBuilder = new EventBroadcastingStrategyBuilder(serviceProvider);

        broadcastingStrategyConfiguration(strategyBuilder);

        var strategy = strategyBuilder.Build(relevantObserverRegistrations.DistinctBy(r => r.ObserverId).ToList());

        var dispatcher = new ReceiverDispatcher(strategy);

        return new(dispatcher, relevantTransportClientObserversRegistrations);
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }

    private async Task<IDictionary<ConquerorEventObserverId, IReadOnlyCollection<IEventObserverTransportConfiguration>>> GetTransportConfigurations()
    {
        if (transportConfigurationsByObserverId is not null)
        {
            return transportConfigurationsByObserverId;
        }

        await semaphore.WaitAsync().ConfigureAwait(false);

        if (transportConfigurationsByObserverId is not null)
        {
            return transportConfigurationsByObserverId;
        }

        var configurationsByObserverId = new Dictionary<ConquerorEventObserverId, IReadOnlyCollection<IEventObserverTransportConfiguration>>();

        try
        {
            foreach (var registration in registrations)
            {
                var transportBuilder = new EventObserverTransportBuilder(serviceProvider);

                // add default in memory transport configuration for every observer
                _ = transportBuilder.UseInMemory();

                registration.ConfigureTransport?.Invoke(transportBuilder);

                var configurations = transportBuilder.GetConfigurations();

                configurationsByObserverId[registration.ObserverId] = configurations;
            }

            transportConfigurationsByObserverId = configurationsByObserverId;
        }
        finally
        {
            _ = semaphore.Release();
        }

        return transportConfigurationsByObserverId;
    }

    private sealed class ReceiverDispatcher(ConfiguredEventBroadcastingStrategy strategy) : IConquerorEventTransportReceiverDispatcher
    {
        private readonly ConcurrentDictionary<Type, IConquerorEventTransportReceiverDispatcher> genericDispatchers = new();

        public Task DispatchEvent(object evt,
                                  ISet<ConquerorEventObserverId> observersToDispatchTo,
                                  IServiceProvider serviceProvider,
                                  CancellationToken cancellationToken = default)
        {
            var dispatcher = genericDispatchers.GetOrAdd(evt.GetType(), CreateGenericDispatcher);
            return dispatcher.DispatchEvent(evt, observersToDispatchTo, serviceProvider, cancellationToken);
        }

        private IConquerorEventTransportReceiverDispatcher CreateGenericDispatcher(Type eventType)
        {
            var dispatcherType = typeof(GenericDispatcher<>).MakeGenericType(eventType);
            return (IConquerorEventTransportReceiverDispatcher)Activator.CreateInstance(dispatcherType, strategy)!;
        }

        private sealed class GenericDispatcher<TEvent>(ConfiguredEventBroadcastingStrategy strategy) : IConquerorEventTransportReceiverDispatcher
            where TEvent : class
        {
            public Task DispatchEvent(object evt,
                                      ISet<ConquerorEventObserverId> observersToDispatchTo,
                                      IServiceProvider serviceProvider,
                                      CancellationToken cancellationToken = default)
            {
                return strategy.DispatchEvent((TEvent)evt, observersToDispatchTo, serviceProvider, cancellationToken);
            }
        }
    }
}

internal sealed record EventObserverRegistration(ConquerorEventObserverId ObserverId,
                                                 IReadOnlyCollection<Type> ObservedEventTypes,
                                                 Type? ObserverType,
                                                 Func<object, IServiceProvider, CancellationToken, Task>? ObserverFn,
                                                 Action<IEventObserverPipelineBuilder>? ConfigurePipeline,
                                                 Action<IEventObserverTransportBuilder>? ConfigureTransport);
