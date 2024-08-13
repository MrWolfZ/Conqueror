using System;

namespace Conqueror;

public interface IStreamingRequestPipelineBuilder
{
    IServiceProvider ServiceProvider { get; }

    IStreamingRequestPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IStreamingRequestMiddleware;

    IStreamingRequestPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>;

    IStreamingRequestPipelineBuilder Without<TMiddleware>()
        where TMiddleware : IStreamingRequestMiddleware;

    IStreamingRequestPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>;

    IStreamingRequestPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>;

    IStreamingRequestPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>;

    IStreamingRequestPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>;
}
