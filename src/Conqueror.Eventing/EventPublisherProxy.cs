using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class EventPublisherProxy<TConfiguration>
    where TConfiguration : Attribute, IConquerorEventTransportConfigurationAttribute
{
    private readonly Action<IEventPublisherPipelineBuilder>? configurePipeline;
    private readonly Type publisherType;

    public EventPublisherProxy(Type publisherType, Action<IEventPublisherPipelineBuilder>? configurePipeline)
    {
        this.configurePipeline = configurePipeline;
        this.publisherType = publisherType;
    }

    public async Task PublishEvent<TEvent>(TEvent evt, TConfiguration configurationAttribute, IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var pipelineBuilder = new EventPublisherPipelineBuilder(serviceProvider);

        configurePipeline?.Invoke(pipelineBuilder);

        var pipeline = pipelineBuilder.Build<TConfiguration>();

        // TODO: add context support
        // var pipeline = pipelineBuilder.Build(conquerorContext);

        await pipeline.Execute(serviceProvider, publisherType, configurationAttribute, evt, cancellationToken).ConfigureAwait(false);
    }
}
