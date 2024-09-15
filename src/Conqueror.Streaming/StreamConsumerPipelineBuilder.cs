using System;
using System.Collections.Generic;

namespace Conqueror.Streaming;

internal sealed class StreamConsumerPipelineBuilder(
    IServiceProvider serviceProvider,
    StreamConsumerMiddlewareRegistry producerMiddlewareRegistry) : IStreamConsumerPipelineBuilder
{
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IStreamConsumerMiddlewareInvoker Invoker)> middlewares = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IStreamConsumerPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IStreamConsumerMiddleware
    {
        middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
        return this;
    }

    public IStreamConsumerPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>
    {
        middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
        return this;
    }

    public IStreamConsumerPipelineBuilder Without<TMiddleware>()
        where TMiddleware : IStreamConsumerMiddleware
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

    public IStreamConsumerPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>
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

    public IStreamConsumerPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(_ => configuration);
    }

    public IStreamConsumerPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(c =>
        {
            configure(c);
            return c;
        });
    }

    public IStreamConsumerPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IStreamConsumerMiddleware<TConfiguration>
    {
        var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

        if (index < 0)
        {
            throw new InvalidOperationException($"middleware ${typeof(TMiddleware).Name} cannot be configured for this pipeline since it is not used");
        }

        middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!), GetInvoker<TMiddleware>());
        return this;
    }

    public StreamConsumerPipeline Build(IConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }

    private IStreamConsumerMiddlewareInvoker GetInvoker<TMiddleware>()
        where TMiddleware : IStreamConsumerMiddlewareMarker
    {
        if (producerMiddlewareRegistry.GetStreamConsumerMiddlewareInvoker<TMiddleware>() is { } invoker)
        {
            return invoker;
        }

        throw new InvalidOperationException($"trying to use unregistered middleware type '{typeof(TMiddleware).Name}' in pipeline; ensure that the middleware is registered in the DI container");
    }
}
