using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class DelegateEventNotificationHandler<TEventNotification>(
    EventNotificationHandlerFn<TEventNotification> handlerFn,
    IServiceProvider serviceProvider)
    : IEventNotificationHandler<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public async Task Handle(TEventNotification notification, CancellationToken cancellationToken = default)
    {
        await handlerFn(notification, serviceProvider, cancellationToken).ConfigureAwait(false);
    }
}
