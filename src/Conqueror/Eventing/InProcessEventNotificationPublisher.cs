using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal sealed class InProcessEventNotificationPublisher<TEventNotification>(
    IEventNotificationBroadcastingStrategy broadcastingStrategy)
    : IEventNotificationPublisher<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public string TransportTypeName => ConquerorConstants.InProcessTransportName;

    public Task Publish(TEventNotification notification,
                        IServiceProvider serviceProvider,
                        ConquerorContext conquerorContext,
                        CancellationToken cancellationToken)
    {
        return serviceProvider.GetRequiredService<InProcessEventNotificationReceiver>()
                              .Broadcast(notification, serviceProvider, broadcastingStrategy, TransportTypeName, cancellationToken);
    }
}
