using System;

namespace Conqueror;

public interface IStreamConsumerPipelineBuilder
{
    IServiceProvider ServiceProvider { get; }

    IStreamConsumerPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IStreamConsumerMiddleware;

    IStreamConsumerPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>;

    IStreamConsumerPipelineBuilder Without<TMiddleware>()
        where TMiddleware : IStreamConsumerMiddleware;

    IStreamConsumerPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>;

    IStreamConsumerPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>;

    IStreamConsumerPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>;

    IStreamConsumerPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>;
}
