using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Conqueror.Eventing;

internal sealed class EventNotificationReceiverBuilder(
    IServiceProvider serviceProvider,
    IEventNotificationTransportRegistry registry,
    IReadOnlyCollection<Type> eventNotificationTypes)
    : IEventNotificationReceiver
{
    private readonly Dictionary<Type, Dictionary<Type, IEventNotificationReceiverConfiguration>> configurationsByNotificationType
        = eventNotificationTypes.ToDictionary(t => t, _ => new Dictionary<Type, IEventNotificationReceiverConfiguration>());

    private int buildCount;

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IReadOnlyCollection<Type> EventNotificationTypes { get; } = eventNotificationTypes;

    private bool IsBuilt => buildCount > 0;

    public IEventNotificationReceiver<TEventNotification> For<TEventNotification>()
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        Debug.Assert(!IsBuilt, "cannot call For<TEventNotification> after GetConfigurationsByNotificationType() has been called");

        if (!configurationsByNotificationType.TryGetValue(typeof(TEventNotification), out var configurationByType))
        {
            throw new InvalidOperationException($"event notification type '{typeof(TEventNotification)}' is not being handled by this receiver");
        }

        return new EventNotificationReceiverBuilder<TEventNotification>(configurationByType);
    }

    public void UseTransport<TTypesInjector>(Action<TTypesInjector> configure)
        where TTypesInjector : class, IEventNotificationTypesInjector
    {
        Debug.Assert(!IsBuilt, "cannot call UseTransport<TTypeInjector> after GetConfigurationsByNotificationType() has been called");

        foreach (var notificationType in EventNotificationTypes)
        {
            var injector = registry.GetTypesInjectorForEventNotificationType<TTypesInjector>(notificationType);

            if (injector is not null)
            {
                configure(injector);
            }
        }
    }

    public IReadOnlyDictionary<Type, IReadOnlyCollection<IEventNotificationReceiverConfiguration>> GetConfigurationsByNotificationType()
    {
        var wasAlreadyBuilt = Interlocked.Increment(ref buildCount) > 1;

        Debug.Assert(!wasAlreadyBuilt, "cannot call GetConfigurationsByNotificationType() multiple times");

        return configurationsByNotificationType.ToDictionary(
            t => t.Key, IReadOnlyCollection<IEventNotificationReceiverConfiguration> (t) =>
                t.Value.Count == 0 ? [InProcessEventNotificationReceiverConfiguration.Instance] : t.Value.Values);
    }
}

internal sealed class EventNotificationReceiverBuilder<TEventNotification>(Dictionary<Type, IEventNotificationReceiverConfiguration> configurationByType)
    : IEventNotificationReceiver<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public void SetConfiguration<TConfiguration>(TConfiguration configuration)
        where TConfiguration : IEventNotificationReceiverConfiguration
    {
        configurationByType[typeof(TConfiguration)] = configuration;
    }

    public void UpdateConfiguration<TConfiguration>(Action<TConfiguration> updateConfiguration)
        where TConfiguration : IEventNotificationReceiverConfiguration
    {
        if (!configurationByType.TryGetValue(typeof(TConfiguration), out var configuration))
        {
            throw new InvalidOperationException($"configuration for type '{typeof(TConfiguration)}' does not exist");
        }

        updateConfiguration((TConfiguration)configuration);
    }
}
