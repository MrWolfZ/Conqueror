using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class InMemoryEventPublishingConfiguredStrategy(
    IConquerorInMemoryEventPublishingStrategy defaultStrategy,
    Dictionary<Type, IConquerorInMemoryEventPublishingStrategy> strategyByEventType,
    IReadOnlyCollection<EventObserverRegistration> observerRegistrations)
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<(Type EventType, EventObserverRegistration Registration)>> observerRegistrationsByEventType = new();

    public Task DispatchEvent<TEvent>(TEvent evt, ISet<ConquerorEventObserverId> observersToDispatchTo, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var registrations = observerRegistrationsByEventType.GetOrAdd(typeof(TEvent), GetObserverRegistrationsForEventType);

        var strategy = strategyByEventType.TryGetValue(typeof(TEvent), out var s) ? s : defaultStrategy;

        var observers = new List<IEventObserver<TEvent>>();

        foreach (var (eventType, registration) in registrations)
        {
            if (!observersToDispatchTo.Contains(registration.ObserverId))
            {
                continue;
            }

            if (registration.ObserverType is not null)
            {
                var observer = new EventObserverProxy<TEvent>(serviceProvider, registration.ConfigurePipeline, registration.ObserverType, eventType);
                observers.Add(observer);
            }
            else if (registration.ObserverFn is not null)
            {
                var delegateObserver = new DelegateEventObserver<TEvent>(registration.ObserverFn, registration.ConfigurePipeline, serviceProvider, eventType);
                observers.Add(delegateObserver);
            }
            else
            {
                // defensive programming; this code should never be reached
                throw new InvalidOperationException("observer registration had neither observer type nor observer function set");
            }
        }

        return strategy.PublishEvent(observers, evt, cancellationToken);
    }

    private IReadOnlyCollection<(Type EventType, EventObserverRegistration Registration)> GetObserverRegistrationsForEventType(Type eventType)
    {
        return (from r in observerRegistrations
                from et in r.ObservedEventTypes
                where eventType.IsAssignableTo(et)
                select (et, r))
            .ToList();
    }
}
