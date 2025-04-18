using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Eventing;

internal sealed class EventNotificationHandlerProxy<TEventNotification>(
    IServiceProvider serviceProvider,
    EventNotificationPublisherFactory<TEventNotification> transportPublisherField,
    Action<IEventNotificationPipeline<TEventNotification>>? configurePipelineField,
    EventNotificationTransportRole transportRole)
    : IConfigurableEventNotificationHandler<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    public async Task Handle(TEventNotification notification, CancellationToken cancellationToken = default)
    {
        using var conquerorContext = serviceProvider.GetRequiredService<IConquerorContextAccessor>().CloneOrCreate();
        var notificationIdFactory = serviceProvider.GetRequiredService<IEventNotificationIdFactory>();

        var originalEventNotificationId = conquerorContext.GetEventNotificationId();

        // ensure that a notification ID is available for the transport client factory
        if (originalEventNotificationId is null)
        {
            conquerorContext.SetEventNotificationId(notificationIdFactory.GenerateId());
        }

        var transportClient = await transportPublisherField.Create(serviceProvider, conquerorContext).ConfigureAwait(false);

        var transportType = new EventNotificationTransportType(transportClient.TransportTypeName, transportRole);

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
                                     transportClient,
                                     transportType,
                                     cancellationToken)
                            .ConfigureAwait(false);
    }

    public IEventNotificationHandler<TEventNotification> WithPipeline(Action<IEventNotificationPipeline<TEventNotification>> configurePipeline)
        => new EventNotificationHandlerProxy<TEventNotification>(
            serviceProvider,
            transportPublisherField,
            pipeline =>
            {
                configurePipelineField?.Invoke(pipeline);
                configurePipeline(pipeline);
            },
            transportRole);

    public IEventNotificationHandler<TEventNotification> WithPublisher(ConfigureEventNotificationPublisher<TEventNotification> configurePublisher)
        => new EventNotificationHandlerProxy<TEventNotification>(
            serviceProvider,
            new(configurePublisher),
            configurePipelineField,
            transportRole);

    public IEventNotificationHandler<TEventNotification> WithPublisher(ConfigureEventNotificationPublisherAsync<TEventNotification> configurePublisher)
        => new EventNotificationHandlerProxy<TEventNotification>(
            serviceProvider,
            new(configurePublisher),
            configurePipelineField,
            transportRole);
}
