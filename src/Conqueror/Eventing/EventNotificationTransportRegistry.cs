using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventNotificationTransportRegistry(
    IServiceProvider serviceProvider,
    IEnumerable<EventNotificationHandlerRegistration> registrations)
    : IEventNotificationTransportRegistry, IDisposable
{
    private readonly List<EventNotificationHandlerRegistration> allRegistrations = registrations.ToList();
    private readonly ConcurrentDictionary<Type, List<IEventNotificationReceiverHandlerInvoker>> invokersByInjectorType = new();
    private readonly SemaphoreSlim semaphore = new(1);
    private readonly ConcurrentDictionary<Type, IEventNotificationTypesInjector?> typesInjectorsByNotificationType = new();

    private IReadOnlyCollection<(EventNotificationHandlerRegistration Registration, IReadOnlyCollection<IEventNotificationReceiverConfiguration> Configurations)>? receivers;

    public TTypesInjector? GetTypesInjectorForEventNotificationType<TTypesInjector>(Type eventNotificationType)
        where TTypesInjector : class, IEventNotificationTypesInjector
    {
        return (TTypesInjector?)typesInjectorsByNotificationType.GetOrAdd(eventNotificationType,
                                                                          t => allRegistrations.FirstOrDefault(r => r.EventNotificationType == t)?
                                                                                               .TypeInjectors
                                                                                               .OfType<TTypesInjector>()
                                                                                               .FirstOrDefault());
    }

    public async Task<IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker<TTypesInjector, TReceiverConfiguration>>> GetEventNotificationInvokersForReceiver<TTypesInjector, TReceiverConfiguration>(
        CancellationToken cancellationToken)
        where TTypesInjector : class, IEventNotificationTypesInjector
        where TReceiverConfiguration : class, IEventNotificationReceiverConfiguration
    {
        var registrationsWithReceiverConfigurations = await ConfigureReceivers(cancellationToken).ConfigureAwait(false);

        var entries = invokersByInjectorType.GetOrAdd(typeof(TTypesInjector),
                                                      _ => [..PopulateEventNotificationInvokersForReceiver<TTypesInjector, TReceiverConfiguration>(registrationsWithReceiverConfigurations)]);

        return entries.OfType<IEventNotificationReceiverHandlerInvoker<TTypesInjector, TReceiverConfiguration>>().ToList();
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }

    private static IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker> PopulateEventNotificationInvokersForReceiver<TTypesInjector, TReceiverConfiguration>(
        IReadOnlyCollection<(EventNotificationHandlerRegistration Registration, IReadOnlyCollection<IEventNotificationReceiverConfiguration> Configurations)> registrationsWithReceiverConfigurations)
        where TTypesInjector : class, IEventNotificationTypesInjector
        where TReceiverConfiguration : class, IEventNotificationReceiverConfiguration
    {
        var invokers = from r in registrationsWithReceiverConfigurations
                       let typesInjector = r.Registration.TypeInjectors.OfType<TTypesInjector>().FirstOrDefault()
                       where typesInjector is not null
                       let configuration = r.Configurations.OfType<TReceiverConfiguration>().FirstOrDefault()
                       where configuration is not null
                       let invoker = new EventNotificationHandlerInvokerForEventNotificationType<TTypesInjector, TReceiverConfiguration>(r.Registration, typesInjector, configuration)
                       group invoker by r.Registration.HandlerType
                       into g
                       select new EventNotificationReceiverHandlerInvoker<TTypesInjector, TReceiverConfiguration>([..g]);

        return invokers.ToList();
    }

    private async Task<IReadOnlyCollection<(EventNotificationHandlerRegistration Registration, IReadOnlyCollection<IEventNotificationReceiverConfiguration> Configurations)>> ConfigureReceivers(CancellationToken cancellationToken)
    {
        if (receivers is not null)
        {
            return receivers;
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (receivers is not null)
            {
                return receivers;
            }

            var configuredHandlerTypes = new HashSet<Type>();
            var result = new List<(EventNotificationHandlerRegistration, IReadOnlyCollection<IEventNotificationReceiverConfiguration>)>();

            foreach (var g in allRegistrations.GroupBy(r => r.HandlerType))
            {
                var handlerType = g.Key;

                // handle delegates
                if (handlerType is null)
                {
                    result.AddRange(g.Select(r => (r, (IReadOnlyCollection<IEventNotificationReceiverConfiguration>)new List<IEventNotificationReceiverConfiguration> { InProcessEventNotificationReceiverConfiguration.Instance })));
                    continue;
                }

                var receiver = new EventNotificationReceiverBuilder(serviceProvider, this, g.Select(r => r.EventNotificationType).ToList());

                foreach (var registration in g)
                {
                    if (handlerType is not null && configuredHandlerTypes.Add(handlerType) && registration.ConfigureReceiver is not null)
                    {
                        await registration.ConfigureReceiver(receiver).ConfigureAwait(false);
                    }
                }

                var configurationsByNotificationType = receiver.GetConfigurationsByNotificationType();

                result.AddRange(g.Select(r => (r, configurationsByNotificationType[r.EventNotificationType])));
            }

            receivers = result;
            return result;
        }
        finally
        {
            _ = semaphore.Release();
        }
    }
}

internal sealed record EventNotificationHandlerRegistration(
    Type EventNotificationType,
    Type? HandlerType,
    Delegate? HandlerFn,
    IEventNotificationHandlerInvoker Invoker,
    Func<IEventNotificationReceiver, Task>? ConfigureReceiver,
    IReadOnlyCollection<IEventNotificationTypesInjector> TypeInjectors);
