using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryPipelineBuilder : IQueryPipelineBuilder
{
    private readonly IReadOnlyDictionary<Type, IQueryMiddlewareInvoker> middlewareInvokersByMiddlewareTypes;
    private readonly List<(Type MiddlewareType, object? MiddlewareConfiguration, IQueryMiddlewareInvoker Invoker)> middlewares = new();

    public QueryPipelineBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        middlewareInvokersByMiddlewareTypes = serviceProvider.GetRequiredService<QueryMiddlewareRegistry>().GetQueryMiddlewareInvokers();
    }

    public IServiceProvider ServiceProvider { get; }

    public IQueryPipelineBuilder Use<TMiddleware>()
        where TMiddleware : IQueryMiddleware
    {
        middlewares.Add((typeof(TMiddleware), null, GetInvoker<TMiddleware>()));
        return this;
    }

    public IQueryPipelineBuilder Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IQueryMiddleware<TConfiguration>
    {
        middlewares.Add((typeof(TMiddleware), configuration, GetInvoker<TMiddleware>()));
        return this;
    }

    public IQueryPipelineBuilder Without<TMiddleware>()
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

    public IQueryPipelineBuilder Without<TMiddleware, TConfiguration>()
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

    public IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IQueryMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(_ => configuration);
    }

    public IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IQueryMiddleware<TConfiguration>
    {
        return Configure<TMiddleware, TConfiguration>(c =>
        {
            configure(c);
            return c;
        });
    }

    public IQueryPipelineBuilder Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
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

    public QueryPipeline Build()
    {
        return new(ServiceProvider.GetRequiredService<QueryContextAccessor>(),
                   ServiceProvider.GetRequiredService<IConquerorContextAccessor>(),
                   middlewares);
    }

    private IQueryMiddlewareInvoker GetInvoker<TMiddleware>()
    {
        if (!middlewareInvokersByMiddlewareTypes.TryGetValue(typeof(TMiddleware), out var invoker))
        {
            throw new InvalidOperationException(
                $"trying to use unregistered middleware type '{typeof(TMiddleware).Name}' in pipeline; ensure that the middleware is registered in the DI container");
        }

        return invoker;
    }
}
