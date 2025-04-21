using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class InProcessEventNotificationReceiver(EventNotificationTransportRegistry registry, IServiceProvider serviceProviderField)
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker<IDefaultEventNotificationTypesInjector>>> invokersByNotificationType = new();

    public async Task Broadcast<TEventNotification>(TEventNotification notification,
                                                    IServiceProvider serviceProvider,
                                                    IEventNotificationBroadcastingStrategy broadcastingStrategy,
                                                    string transportTypeName,
                                                    CancellationToken cancellationToken)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        var relevantInvokers = invokersByNotificationType.GetOrAdd(notification.GetType(), GetEventNotificationInvokers);

        await broadcastingStrategy.BroadcastEventNotification(relevantInvokers, serviceProvider, notification, transportTypeName, cancellationToken)
                                  .ConfigureAwait(false);
    }

    private IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker<IDefaultEventNotificationTypesInjector>> GetEventNotificationInvokers(Type notificationType)
    {
        return registry.GetEventNotificationInvokersForReceiver<IDefaultEventNotificationTypesInjector>()
                       .Where(i => notificationType.IsAssignableTo(i.EventNotificationType))
                       .Where(i => i.TypesInjector.CreateWithEventNotificationTypes(new Injectable(serviceProviderField)))
                       .ToList();
    }

    private sealed class Injectable(IServiceProvider serviceProvider) : IDefaultEventNotificationTypesInjectable<bool>
    {
        bool IDefaultEventNotificationTypesInjectable<bool>.WithInjectedTypes<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            TEventNotification,
            TGeneratedHandlerInterface,
            TGeneratedHandlerAdapter>()
        {
            // delegate handlers are always active
            return true;
        }

        bool IDefaultEventNotificationTypesInjectable<bool>.WithInjectedTypes<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            TEventNotification,
            TGeneratedHandlerInterface,
            TGeneratedHandlerAdapter,
            THandler>()
        {
            var receiver = new Receiver<TEventNotification>(serviceProvider);
            THandler.ConfigureInProcessReceiver(receiver);
            return receiver.IsEnabled;
        }
    }

    private sealed class Receiver<TEventNotification>(IServiceProvider serviceProvider) : IInProcessEventNotificationReceiver<TEventNotification>
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public bool IsEnabled { get; private set; }

        public void Enable() => IsEnabled = true;

        public void Disable() => IsEnabled = false;
    }
}
