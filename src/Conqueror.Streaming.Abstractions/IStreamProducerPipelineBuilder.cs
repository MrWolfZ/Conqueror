using System;

namespace Conqueror;

public interface IStreamProducerPipelineBuilder
{
    IServiceProvider ServiceProvider { get; }

    IStreamProducerPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IStreamProducerMiddleware;

    IStreamProducerPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>;

    IStreamProducerPipelineBuilder Without<TMiddleware>()
        where TMiddleware : IStreamProducerMiddleware;

    IStreamProducerPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>;

    IStreamProducerPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>;

    IStreamProducerPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>;

    IStreamProducerPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>;
}
