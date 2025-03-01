using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Publishing;

internal sealed class EventPublisherProxy<TConfiguration>(Type publisherType, Action<IEventPublisherPipelineBuilder>? configurePipeline)
    where TConfiguration : Attribute, IConquerorEventTransportConfigurationAttribute
{
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
