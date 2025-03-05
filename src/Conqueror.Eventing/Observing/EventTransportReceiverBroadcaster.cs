using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing.Observing;

internal sealed class EventTransportReceiverBroadcaster(
    IEnumerable<EventObserverRegistration> registrations,
    IEnumerable<EventObserverDelegateRegistration> delegateRegistrations) : IConquerorEventTransportReceiverBroadcaster
{
    private readonly ConcurrentDictionary<Type, IConquerorEventTransportReceiverBroadcaster> genericDispatchers = new();

    private IReadOnlyCollection<EventObserverRegistration> Registrations { get; } = registrations.ToList();

    private IReadOnlyCollection<EventObserverDelegateRegistration> DelegateRegistrations { get; } = delegateRegistrations.ToList();

    public Task Broadcast(object evt, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dispatcher = genericDispatchers.GetOrAdd(evt.GetType(), CreateGenericDispatcher);
        return dispatcher.Broadcast(evt, serviceProvider, cancellationToken);
    }

    private IConquerorEventTransportReceiverBroadcaster CreateGenericDispatcher(Type eventType)
    {
        var dispatcherType = typeof(GenericBroadcaster<>).MakeGenericType(eventType);
        return (IConquerorEventTransportReceiverBroadcaster)Activator.CreateInstance(dispatcherType, GetObserverRegistrations())!;

        IReadOnlyCollection<(EventObserverRegistration? Registration, EventObserverDelegateRegistration? DelegateRegistration)> GetObserverRegistrations()
        {
            var regs = from r in Registrations
                       where eventType.IsAssignableTo(r.EventType)
                       select (r, (EventObserverDelegateRegistration?)null);

            var delegateRegs = from r in DelegateRegistrations
                               where eventType.IsAssignableTo(r.EventType)
                               select ((EventObserverRegistration?)null, r);

            return regs.Concat(delegateRegs).ToList();
        }
    }

    private sealed class GenericBroadcaster<TEvent>(
        IReadOnlyCollection<(EventObserverRegistration? Registration, EventObserverDelegateRegistration? DelegateRegistration)> registrations)
        : IConquerorEventTransportReceiverBroadcaster
        where TEvent : class
    {
        public Task Broadcast(object evt, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var observers = new List<IEventObserver<TEvent>>();

            foreach (var (registration, delegateRegistration) in registrations)
            {
                if (registration?.ObserverType is not null)
                {
                    var observer = new EventObserverProxy<TEvent>(serviceProvider,
                                                                  registration.ConfigurePipeline,
                                                                  registration.ObserverType,
                                                                  registration.EventType);
                    observers.Add(observer);
                }
                else if (delegateRegistration?.ObserverFn is not null)
                {
                    var delegateObserver = new DelegateEventObserver<TEvent>(delegateRegistration.ObserverFn,
                                                                             delegateRegistration.ConfigurePipeline,
                                                                             serviceProvider,
                                                                             delegateRegistration.EventType);
                    observers.Add(delegateObserver);
                }
                else
                {
                    // defensive programming; this code should never be reached
                    throw new InvalidOperationException("observer registration had neither observer type nor observer function set");
                }
            }

            var broadcastingStrategy = serviceProvider.GetRequiredService<IConquerorEventBroadcastingStrategy>();
            return broadcastingStrategy.BroadcastEvent(observers, (TEvent)evt, cancellationToken);
        }
    }
}
