using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationReceiver
{
    IServiceProvider ServiceProvider { get; }

    IEventNotificationReceiver<TEventNotification> For<TEventNotification>()
        where TEventNotification : class, IEventNotification<TEventNotification>;

    IReadOnlyCollection<Type> EventNotificationTypes { get; }

    TResult UseTransport<TTypeInjector, TResult>(Func<TTypeInjector, TResult> configure);
}

public interface IEventNotificationReceiver<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    IReadOnlyCollection<IEventNotificationReceiverConfiguration> Configurations { get; }

    void SetConfiguration<TConfiguration>(TConfiguration configuration)
        where TConfiguration : IEventNotificationReceiverConfiguration;

    void UpdateConfiguration<TConfiguration>(Action<TConfiguration> updateConfiguration)
        where TConfiguration : IEventNotificationReceiverConfiguration;
}

public interface IEventNotificationReceiverConfiguration;
