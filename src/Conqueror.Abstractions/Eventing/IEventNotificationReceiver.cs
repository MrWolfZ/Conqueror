using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationReceiver
{
    IServiceProvider ServiceProvider { get; }

    IReadOnlyCollection<Type> EventNotificationTypes { get; }

    IEventNotificationReceiver<TEventNotification> For<TEventNotification>()
        where TEventNotification : class, IEventNotification<TEventNotification>;

    void UseTransport<TTypesInjector>(Action<TTypesInjector> configure)
        where TTypesInjector : class, IEventNotificationTypesInjector;

    /// <summary>
    ///     Must only be called once after all receivers have been configured.
    /// </summary>
    internal IReadOnlyDictionary<Type, IReadOnlyCollection<IEventNotificationReceiverConfiguration>> GetConfigurationsByNotificationType();
}

public interface IEventNotificationReceiver<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    void SetConfiguration<TConfiguration>(TConfiguration configuration)
        where TConfiguration : IEventNotificationReceiverConfiguration;

    void UpdateConfiguration<TConfiguration>(Action<TConfiguration> updateConfiguration)
        where TConfiguration : IEventNotificationReceiverConfiguration;
}

public interface IEventNotificationReceiverConfiguration;
