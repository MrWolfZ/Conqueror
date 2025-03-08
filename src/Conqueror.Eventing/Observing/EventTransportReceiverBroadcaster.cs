using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing.Observing;

internal sealed class EventTransportReceiverBroadcaster(IEnumerable<IEventObserverInvoker> invokers) : IEventTransportReceiverBroadcaster
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<IEventObserverInvoker>> invokersByEventType = new();

    private IReadOnlyCollection<IEventObserverInvoker> Invokers { get; } = invokers.ToList();

    public Task Broadcast(object evt, EventTransportAttribute attribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var invokersForEventType = invokersByEventType.GetOrAdd(evt.GetType(), GetInvokersForEventType);
        var observerFns = invokersForEventType.Select(i => (EventObserverFn)((e, ct) => i.Invoke(serviceProvider, attribute, e, ct))).ToList();
        var broadcastingStrategy = serviceProvider.GetRequiredService<IEventBroadcastingStrategy>();
        return broadcastingStrategy.BroadcastEvent(observerFns, serviceProvider, evt, cancellationToken);
    }

    private IReadOnlyCollection<IEventObserverInvoker> GetInvokersForEventType(Type eventType) =>
        Invokers.Where(i => i.AcceptsEventType(eventType)).ToList();
}
