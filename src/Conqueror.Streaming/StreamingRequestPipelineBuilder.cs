using System;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestPipelineBuilder : IStreamingRequestPipelineBuilder
{
    private readonly StreamingRequestMiddlewareRegistry requestMiddlewareRegistry;

    public StreamingRequestPipelineBuilder(IServiceProvider serviceProvider, StreamingRequestMiddlewareRegistry requestMiddlewareRegistry)
    {
        this.requestMiddlewareRegistry = requestMiddlewareRegistry;
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public IStreamingRequestPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IStreamingRequestMiddleware
    {
        return this;
    }

    public IStreamingRequestPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        return this;
    }

    public IStreamingRequestPipelineBuilder Without<TMiddleware>()
        where TMiddleware : IStreamingRequestMiddleware
    {
        return this;
    }

    public IStreamingRequestPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        return this;
    }

    public IStreamingRequestPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        return this;
    }

    public IStreamingRequestPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        return this;
    }

    public IStreamingRequestPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        return this;
    }

    public StreamingRequestPipeline Build(IConquerorContext conquerorContext)
    {
        _ = requestMiddlewareRegistry;
        return new(conquerorContext);
    }
}
