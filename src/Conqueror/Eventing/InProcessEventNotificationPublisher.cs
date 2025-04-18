using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal sealed class InProcessEventNotificationPublisher<TEventNotification>(
    Type handlerType,
    Delegate? configurePipeline)
    : IEventNotificationPublisher<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public string TransportTypeName => ConquerorConstants.InProcessTransportName;

    public Task Publish(TEventNotification notification,
                        IServiceProvider serviceProvider,
                        ConquerorContext conquerorContext,
                        CancellationToken cancellationToken)
    {
        var proxy = new EventNotificationHandlerProxy<TEventNotification>(serviceProvider,
                                                                          new(new HandlerInvoker(handlerType, TransportTypeName)),
                                                                          (Action<IEventNotificationPipeline<TEventNotification>>?)configurePipeline,
                                                                          EventNotificationTransportRole.Receiver);

        return proxy.Handle(notification, cancellationToken);
    }

    private sealed class HandlerInvoker(Type handlerType, string transportTypeName) : IEventNotificationPublisher<TEventNotification>
    {
        public string TransportTypeName => transportTypeName;

        public async Task Publish(TEventNotification notification,
                                  IServiceProvider serviceProvider,
                                  ConquerorContext conquerorContext,
                                  CancellationToken cancellationToken)
        {
            var handler = serviceProvider.GetRequiredService(handlerType);
            await ((IEventNotificationHandler<TEventNotification>)handler).Handle(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
