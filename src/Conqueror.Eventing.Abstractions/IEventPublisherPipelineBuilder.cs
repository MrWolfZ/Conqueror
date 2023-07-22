using System;

namespace Conqueror;

public interface IEventPublisherPipelineBuilder
{
    IServiceProvider ServiceProvider { get; }

    IEventPublisherPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IEventPublisherMiddleware;

    IEventPublisherPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>;

    IEventPublisherPipelineBuilder Without<TMiddleware>()
        where TMiddleware : IEventPublisherMiddleware;

    IEventPublisherPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>;

    IEventPublisherPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>;

    IEventPublisherPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>;

    IEventPublisherPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IEventPublisherMiddleware<TConfiguration>;
}
