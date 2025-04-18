using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventNotificationHandlerInvoker<TEventNotification>(
    Action<IEventNotificationPipeline<TEventNotification>>? configurePipeline,
    EventNotificationHandlerFn<TEventNotification> handlerFn)
    : IEventNotificationHandlerInvoker
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public Task Invoke(IServiceProvider serviceProvider, object notification, string transportTypeName, CancellationToken cancellationToken)
    {
        var proxy = new EventNotificationHandlerProxy<TEventNotification>(serviceProvider,
                                                                          new(new Publisher(handlerFn, transportTypeName)),
                                                                          configurePipeline,
                                                                          EventNotificationTransportRole.Receiver);

        return proxy.Handle((TEventNotification)notification, cancellationToken);
    }

    private sealed class Publisher(EventNotificationHandlerFn<TEventNotification> handlerFn, string transportTypeName) : IEventNotificationPublisher<TEventNotification>
    {
        public string TransportTypeName { get; } = transportTypeName;

        public Task Publish(TEventNotification notification, IServiceProvider serviceProvider, ConquerorContext conquerorContext, CancellationToken cancellationToken)
            => handlerFn(notification, serviceProvider, cancellationToken);
    }
}

internal interface IEventNotificationHandlerInvoker
{
    Task Invoke(
        IServiceProvider serviceProvider,
        object notification,
        string transportTypeName,
        CancellationToken cancellationToken);
}
