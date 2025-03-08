using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventTransportRegistry(IEnumerable<EventObserverRegistration> registrations,
                                             IEnumerable<EventObserverDelegateRegistration> delegateRegistrations) : IEventTransportRegistry
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<(Type EventType, object Attribute)>> queryTypesByTransportAttribute = new();
    private readonly IReadOnlyCollection<EventObserverRegistration> registrations = registrations.ToList();
    private readonly IReadOnlyCollection<EventObserverDelegateRegistration> delegateRegistrations = delegateRegistrations.ToList();

    public IReadOnlyCollection<(Type EventType, TTransportMarkerAttribute Attribute)> GetEventTypesForReceiver<TTransportMarkerAttribute>()
        where TTransportMarkerAttribute : ConquerorEventTransportAttribute
    {
        var entries = queryTypesByTransportAttribute.GetOrAdd(typeof(TTransportMarkerAttribute),
                                                              _ => (from eventType in registrations.Select(r => r.EventType).Concat(delegateRegistrations.Select(r => r.EventType)).Distinct()
                                                                    let attribute = eventType.GetCustomAttribute<TTransportMarkerAttribute>()
                                                                    where attribute != null || typeof(TTransportMarkerAttribute) == typeof(InProcessEventAttribute)
                                                                    select (eventType, (object)attribute ?? new InProcessEventAttribute())).ToList());

        return entries.Select(e => (e.EventType, (TTransportMarkerAttribute)e.Attribute)).ToList();
    }
}

public sealed record EventObserverRegistration(Type EventType, Type ObserverType, Delegate? ConfigurePipeline);

public sealed record EventObserverDelegateRegistration(
    Type EventType,
    Func<object, IServiceProvider, CancellationToken, Task> ObserverFn,
    Delegate? ConfigurePipeline);
