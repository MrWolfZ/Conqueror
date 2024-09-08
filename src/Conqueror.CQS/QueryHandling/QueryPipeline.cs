using System;
using System.Collections.Generic;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryPipeline<TQuery, TResponse> : IQueryPipeline<TQuery, TResponse>
    where TQuery : class
{
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares = new();
    private readonly QueryMiddlewareRegistry queryMiddlewareRegistry;

    public QueryPipeline(IServiceProvider serviceProvider, QueryMiddlewareRegistry queryMiddlewareRegistry)
    {
        this.queryMiddlewareRegistry = queryMiddlewareRegistry;
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public IQueryPipeline<TQuery, TResponse> Use<TMiddleware>()
        where TMiddleware : IQueryMiddleware
    {
        middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
        return this;
    }

    public IQueryPipeline<TQuery, TResponse> Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IQueryMiddleware<TConfiguration>
    {
        middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
        return this;
    }

    public IQueryPipeline<TQuery, TResponse> Without<TMiddleware>()
        where TMiddleware : IQueryMiddleware
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

    public IQueryPipeline<TQuery, TResponse> Without<TMiddleware, TConfiguration>()
        where TMiddleware : IQueryMiddleware<TConfiguration>
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

    public IQueryPipeline<TQuery, TResponse> Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IQueryMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(_ => configuration);
    }

    public IQueryPipeline<TQuery, TResponse> Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IQueryMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(c =>
        {
            configure(c);
            return c;
        });
    }

    public IQueryPipeline<TQuery, TResponse> Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IQueryMiddleware<TConfiguration>
    {
        var index = middlewares.FindIndex(tuple => tuple.MiddlewareType == typeof(TMiddleware));

        if (index < 0)
        {
            throw new InvalidOperationException($"middleware ${typeof(TMiddleware).Name} cannot be configured for this pipeline since it is not used");
        }

        middlewares[index] = (typeof(TMiddleware), configure((TConfiguration)middlewares[index].MiddlewareConfiguration!), GetInvoker<TMiddleware>());
        return this;
    }

    public QueryPipelineRunner Build(IConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }

    private IQueryMiddlewareInvoker GetInvoker<TMiddleware>()
        where TMiddleware : IQueryMiddlewareMarker
    {
        if (queryMiddlewareRegistry.GetQueryMiddlewareInvoker<TMiddleware>() is { } invoker)
        {
            return invoker;
        }

        throw new InvalidOperationException(
            $"trying to use unregistered middleware type '{typeof(TMiddleware).Name}' in pipeline; ensure that the middleware is registered in the DI container");
    }
}
