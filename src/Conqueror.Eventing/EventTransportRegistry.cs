using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Conqueror.Eventing.Observing;

namespace Conqueror.Eventing;

internal sealed class EventTransportRegistry(IEnumerable<IEventObserverInvoker> invokers) : IEventTransportRegistry
{
    private readonly IReadOnlyCollection<Type> allEventTypes = invokers.Select(i => i.EventType).Distinct().ToList();
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<(Type EventType, object Attribute)>> queryTypesByTransportAttribute = new();

    public IReadOnlyCollection<(Type EventType, TTransportMarkerAttribute Attribute)> GetEventTypesForReceiver<TTransportMarkerAttribute>()
        where TTransportMarkerAttribute : EventTransportAttribute
    {
        var entries = queryTypesByTransportAttribute.GetOrAdd(typeof(TTransportMarkerAttribute),
                                                              _ => (from eventType in allEventTypes
                                                                    let attribute = eventType.GetCustomAttribute<TTransportMarkerAttribute>()
                                                                    where attribute != null || typeof(TTransportMarkerAttribute) == typeof(InProcessEventAttribute)
                                                                    select (eventType, (object)attribute ?? new InProcessEventAttribute())).ToList());

        return entries.Select(e => (e.EventType, (TTransportMarkerAttribute)e.Attribute)).ToList();
    }
}
