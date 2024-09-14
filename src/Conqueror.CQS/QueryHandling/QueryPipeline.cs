using System;
using System.Collections.Generic;
using System.Linq;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryPipeline<TQuery, TResponse> : IQueryPipeline<TQuery, TResponse>
    where TQuery : class
{
    private readonly List<IQueryMiddleware<TQuery, TResponse>> middlewares = [];

    public QueryPipeline(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public IServiceProvider ServiceProvider { get; }

    public IQueryPipeline<TQuery, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>
    {
        middlewares.Add(middleware);
        return this;
    }

    public IQueryPipeline<TQuery, TResponse> Without<TMiddleware>()
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>
    {
        while (true)
        {
            var index = middlewares.FindIndex(m => m is TMiddleware);

            if (index < 0)
            {
                return this;
            }

            middlewares.RemoveAt(index);
        }
    }

    public IQueryPipeline<TQuery, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>
    {
        var index = middlewares.FindIndex(m => m is TMiddleware);

        if (index < 0)
        {
            throw new InvalidOperationException($"middleware '${typeof(TMiddleware)}' cannot be configured for this pipeline since it is not used");
        }

        foreach (var middleware in middlewares.OfType<TMiddleware>())
        {
            configure(middleware);
        }

        return this;
    }

    public QueryPipelineRunner<TQuery, TResponse> Build(IConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }
}
