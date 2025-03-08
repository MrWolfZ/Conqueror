using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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

    public Task Broadcast(object evt, ConquerorEventTransportAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var dispatcher = genericDispatchers.GetOrAdd(evt.GetType(), CreateDispatcherForEventType);
        return dispatcher.Broadcast(evt, configurationAttribute, serviceProvider, cancellationToken);
    }

    private IConquerorEventTransportReceiverBroadcaster CreateDispatcherForEventType(Type eventType)
    {
        return (IConquerorEventTransportReceiverBroadcaster)Activator.CreateInstance(typeof(GenericBroadcaster<>).MakeGenericType(eventType), GetObserverRegistrations())!;

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
        public Task Broadcast(object evt, ConquerorEventTransportAttribute configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var observers = new List<IEventObserver<TEvent>>();

            foreach (var (registration, delegateRegistration) in registrations)
            {
                if (registration?.ObserverType is not null)
                {
                    if (registration.EventType == typeof(TEvent))
                    {
                        var observer = new EventObserverProxy<TEvent>(serviceProvider,
                                                                      (Action<IEventPipeline<TEvent>>?)registration.ConfigurePipeline,
                                                                      (e, p, ct) => ((IEventObserver<TEvent>)p.GetRequiredService(registration.ObserverType)).Handle(e, ct),
                                                                      registration.EventType,
                                                                      configurationAttribute);

                        observers.Add(observer);
                    }
                    else
                    {
                        var methodInfo = typeof(GenericBroadcaster<TEvent>).GetMethod(nameof(CreateObserverProxyForSubType), BindingFlags.NonPublic | BindingFlags.Static);

                        if (methodInfo == null)
                        {
                            throw new InvalidOperationException($"could not find method '{nameof(CreateObserverProxyForSubType)}'");
                        }

                        var genericMethod = methodInfo.MakeGenericMethod(typeof(TEvent), registration.EventType);

                        try
                        {
                            var result = genericMethod.Invoke(null, [serviceProvider, registration.ConfigurePipeline, registration.ObserverType, configurationAttribute]);
                            observers.Add((IEventObserver<TEvent>)result!);
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException != null)
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }
                    }
                }
                else if (delegateRegistration?.ObserverFn is not null)
                {
                    if (delegateRegistration.EventType == typeof(TEvent))
                    {
                        var delegateObserver = new EventObserverProxy<TEvent>(serviceProvider,
                                                                              (Action<IEventPipeline<TEvent>>?)delegateRegistration.ConfigurePipeline,
                                                                              delegateRegistration.ObserverFn,
                                                                              delegateRegistration.EventType,
                                                                              configurationAttribute);

                        observers.Add(delegateObserver);
                    }
                    else
                    {
                        var methodInfo = typeof(GenericBroadcaster<TEvent>).GetMethod(nameof(CreateObserverProxyForSubTypeDelegate), BindingFlags.NonPublic | BindingFlags.Static);

                        if (methodInfo == null)
                        {
                            throw new InvalidOperationException($"could not find method '{nameof(CreateObserverProxyForSubTypeDelegate)}'");
                        }

                        var genericMethod = methodInfo.MakeGenericMethod(typeof(TEvent), delegateRegistration.EventType);

                        try
                        {
                            var result = genericMethod.Invoke(null, [serviceProvider, delegateRegistration.ConfigurePipeline, delegateRegistration.ObserverFn, configurationAttribute]);
                            observers.Add((IEventObserver<TEvent>)result!);
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException != null)
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }
                    }
                }
                else
                {
                    // defensive programming; this code should never be reached
                    throw new InvalidOperationException("observer registration had neither observer type nor observer function set");
                }
            }

            var broadcastingStrategy = serviceProvider.GetRequiredService<IConquerorEventBroadcastingStrategy>();
            return broadcastingStrategy.BroadcastEvent(observers, serviceProvider, (TEvent)evt, cancellationToken);
        }

        private static IEventObserver<TResolvedEvent> CreateObserverProxyForSubType<TResolvedEvent, TObservedEvent>(
            IServiceProvider serviceProvider,
            Action<IEventPipeline<TObservedEvent>>? configurePipeline,
            Type observerType,
            ConquerorEventTransportAttribute configurationAttribute)
            where TResolvedEvent : class, TObservedEvent
            where TObservedEvent : class
        {
            return new EventObserverProxy<TResolvedEvent, TObservedEvent>(serviceProvider,
                                                                          configurePipeline,
                                                                          (e, p, ct) => ((IEventObserver<TObservedEvent>)p.GetRequiredService(observerType)).Handle(e, ct),
                                                                          typeof(TObservedEvent),
                                                                          configurationAttribute);
        }

        private static IEventObserver<TResolvedEvent> CreateObserverProxyForSubTypeDelegate<TResolvedEvent, TObservedEvent>(
            IServiceProvider serviceProvider,
            Action<IEventPipeline<TObservedEvent>>? configurePipeline,
            Func<TObservedEvent, IServiceProvider, CancellationToken, Task> observerFn,
            ConquerorEventTransportAttribute configurationAttribute)
            where TResolvedEvent : class, TObservedEvent
            where TObservedEvent : class
        {
            return new EventObserverProxy<TResolvedEvent, TObservedEvent>(serviceProvider,
                                                                          configurePipeline,
                                                                          observerFn,
                                                                          typeof(TObservedEvent),
                                                                          configurationAttribute);
        }
    }
}
