using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class InProcessEventNotificationReceiver(EventNotificationTransportRegistry registry) : IDisposable
{
    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker>> invokersByNotificationType = new();
    private readonly SemaphoreSlim semaphore = new(1);

    private IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker>? invokers;

    public async Task Broadcast<TEventNotification>(TEventNotification notification,
                                                    IServiceProvider serviceProvider,
                                                    IEventNotificationBroadcastingStrategy broadcastingStrategy,
                                                    string transportTypeName,
                                                    CancellationToken cancellationToken)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        var invokers2 = await GetInvokers(cancellationToken).ConfigureAwait(false);

        var relevantInvokers = invokersByNotificationType.GetOrAdd(notification.GetType(),
                                                                   _ => invokers2.Where(i => i.AcceptsEventNotificationType(notification.GetType())).ToList());

        await broadcastingStrategy.BroadcastEventNotification(relevantInvokers, serviceProvider, notification, transportTypeName, cancellationToken)
                                  .ConfigureAwait(false);
    }

    public void Dispose()
    {
        semaphore.Dispose();
    }

    // strictly speaking, this is not necessary, since the registry already caches internally, but since this is
    // on the hot path, we want to avoid any extra calls to the registry if possible
    private async Task<IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker>> GetInvokers(CancellationToken cancellationToken)
    {
        if (invokers is not null)
        {
            return invokers;
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (invokers is not null)
            {
                return invokers;
            }

            invokers = await registry.GetEventNotificationInvokersForReceiver<IDefaultEventNotificationTypesInjector, InProcessEventNotificationReceiverConfiguration>(cancellationToken)
                                     .ConfigureAwait(false);

            return invokers;
        }
        finally
        {
            _ = semaphore.Release();
        }
    }
}

internal sealed record InProcessEventNotificationReceiverConfiguration : IEventNotificationReceiverConfiguration
{
    public static readonly InProcessEventNotificationReceiverConfiguration Instance = new();
}
