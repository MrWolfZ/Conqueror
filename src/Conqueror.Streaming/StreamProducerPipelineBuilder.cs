using System;
using System.Collections.Generic;

namespace Conqueror.Streaming;

internal sealed class StreamProducerPipelineBuilder(
    IServiceProvider serviceProvider,
    StreamProducerMiddlewareRegistry producerMiddlewareRegistry)
    : IStreamProducerPipelineBuilder
{
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IStreamProducerMiddlewareInvoker Invoker)> middlewares = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public IStreamProducerPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IStreamProducerMiddleware
    {
        middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
        return this;
    }

    public IStreamProducerPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>
    {
        middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
        return this;
    }

    public IStreamProducerPipelineBuilder Without<TMiddleware>()
        where TMiddleware : IStreamProducerMiddleware
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

    public IStreamProducerPipelineBuilder Without<TMiddleware, TConfiguration>()
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>
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

    public IStreamProducerPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(_ => configuration);
    }

    public IStreamProducerPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(c =>
        {
            configure(c);
            return c;
        });
    }

    public IStreamProducerPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IStreamProducerMiddleware<TConfiguration>
    {
        var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

        if (index < 0)
        {
            throw new InvalidOperationException($"middleware ${typeof(TMiddleware).Name} cannot be configured for this pipeline since it is not used");
        }

        middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!), GetInvoker<TMiddleware>());
        return this;
    }

    public StreamProducerPipeline Build(IConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }

    private IStreamProducerMiddlewareInvoker GetInvoker<TMiddleware>()
        where TMiddleware : IStreamProducerMiddlewareMarker
    {
        if (producerMiddlewareRegistry.GetStreamProducerMiddlewareInvoker<TMiddleware>() is { } invoker)
        {
            return invoker;
        }

        throw new InvalidOperationException($"trying to use unregistered middleware type '{typeof(TMiddleware).Name}' in pipeline; ensure that the middleware is registered in the DI container");
    }
}
