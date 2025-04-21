using System;
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
}

public interface IEventNotificationReceiverHandlerInvoker<out TTypesInjector> : IEventNotificationReceiverHandlerInvoker
    where TTypesInjector : class, IEventNotificationTypesInjector
{
    Type EventNotificationType { get; }

    Type? HandlerType { get; }

    TTypesInjector TypesInjector { get; }
}
