using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IEventNotificationReceiverHandlerInvoker
{
    Task Invoke<TEventNotification>(
        IServiceProvider serviceProvider,
        TEventNotification notification,
        string transportTypeName,
        CancellationToken cancellationToken)
        where TEventNotification : class, IEventNotification<TEventNotification>;

    bool AcceptsEventNotificationType(Type eventNotificationType);
}

public interface IEventNotificationReceiverHandlerInvoker<TTypesInjector, TReceiverConfiguration> : IEventNotificationReceiverHandlerInvoker
    where TTypesInjector : class, IEventNotificationTypesInjector
    where TReceiverConfiguration : class, IEventNotificationReceiverConfiguration
{
    IReadOnlyCollection<(TTypesInjector TypesInjector, TReceiverConfiguration ReceiverConfiguration)> HandledEventNotificationTypes { get; }
}
