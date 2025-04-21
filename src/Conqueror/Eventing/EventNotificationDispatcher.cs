using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal sealed class EventNotificationDispatcher<TEventNotification>(
    IServiceProvider serviceProvider,
    EventNotificationPublisherFactory<TEventNotification> publisherFactory,
    Action<IEventNotificationPipeline<TEventNotification>>? configurePipelineField,
    EventNotificationTransportRole transportRole)
    : IEventNotificationDispatcher<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public async Task Dispatch(TEventNotification notification, CancellationToken cancellationToken)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();
        var notificationIdFactory = serviceProvider.GetRequiredService<IEventNotificationIdFactory>();

        var originalEventNotificationId = conquerorContext.GetEventNotificationId();

        // ensure that a notification ID is available for the transport client factory
        if (originalEventNotificationId is null)
        {
            conquerorContext.SetEventNotificationId(notificationIdFactory.GenerateId());
        }

        var publisher = await publisherFactory.Create(serviceProvider, conquerorContext).ConfigureAwait(false);

        var transportType = new EventNotificationTransportType(publisher.TransportTypeName, transportRole);

        // if we are an in-process client, make sure to create a new notification ID for this execution if
        // we were called from within the call context of another handler
        if (originalEventNotificationId is not null && transportType.IsInProcess() && transportRole == EventNotificationTransportRole.Publisher)
        {
            conquerorContext.SetEventNotificationId(notificationIdFactory.GenerateId());
        }

        var pipeline = new EventNotificationPipeline<TEventNotification>(serviceProvider, conquerorContext, transportType);

        configurePipelineField?.Invoke(pipeline);

        var pipelineRunner = pipeline.Build(conquerorContext);

        await pipelineRunner.Execute(serviceProvider,
                                     notification,
                                     publisher,
                                     transportType,
                                     cancellationToken)
                            .ConfigureAwait(false);
    }

    public IEventNotificationDispatcher<TEventNotification> WithPipeline(Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        => new EventNotificationDispatcher<TEventNotification>(
            serviceProvider,
            publisherFactory,
            pipeline =>
            {
                configurePipelineField?.Invoke(pipeline);
                configurePipeline(pipeline);
            },
            transportRole);

    public IEventNotificationDispatcher<TEventNotification> WithPublisher(ConfigureEventNotificationPublisher<TEventNotification> configurePublisher)
        => new EventNotificationDispatcher<TEventNotification>(
            serviceProvider,
            new(configurePublisher),
            configurePipelineField,
            transportRole);

    public IEventNotificationDispatcher<TEventNotification> WithPublisher(ConfigureEventNotificationPublisherAsync<TEventNotification> configurePublisher)
        => new EventNotificationDispatcher<TEventNotification>(
            serviceProvider,
            new(configurePublisher),
            configurePipelineField,
            transportRole);
}
