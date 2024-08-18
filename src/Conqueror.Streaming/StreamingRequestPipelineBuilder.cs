using System;
using System.Collections.Generic;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestPipelineBuilder : IStreamingRequestPipelineBuilder
{
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IStreamingRequestMiddlewareInvoker Invoker)> middlewares = new();
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
        middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
        return this;
    }

    public IStreamingRequestPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
        return this;
    }

    public IStreamingRequestPipelineBuilder Without<TMiddleware>()
        where TMiddleware : IStreamingRequestMiddleware
    {
        while (true)
        {
            var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

            if (index < 0)
            {
                return this;
            }

            middlewares.RemoveAt(index);
        }
    }

    public IStreamingRequestPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        while (true)
        {
            var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

            if (index < 0)
            {
                return this;
            }

            middlewares.RemoveAt(index);
        }
    }

    public IStreamingRequestPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(_ => configuration);
    }

    public IStreamingRequestPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(c =>
        {
            configure(c);
            return c;
        });
    }

    public IStreamingRequestPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IStreamingRequestMiddleware<TConfiguration>
    {
        var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

        if (index < 0)
        {
            throw new InvalidOperationException($"middleware ${typeof(TMiddleware).Name} cannot be configured for this pipeline since it is not used");
        }

        middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!), GetInvoker<TMiddleware>());
        return this;
    }

    public StreamingRequestPipeline Build(IConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }

    private IStreamingRequestMiddlewareInvoker GetInvoker<TMiddleware>()
        where TMiddleware : IStreamingRequestMiddlewareMarker
    {
        if (requestMiddlewareRegistry.GetStreamingRequestMiddlewareInvoker<TMiddleware>() is { } invoker)
        {
            return invoker;
        }

        throw new InvalidOperationException($"trying to use unregistered middleware type '{typeof(TMiddleware).Name}' in pipeline; ensure that the middleware is registered in the DI container");
    }
}
