using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryPipeline<TQuery, TResponse>(
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    QueryTransportType transportType)
    : IQueryPipeline<TQuery, TResponse>
    where TQuery : class
{
    private readonly List<IQueryMiddleware<TQuery, TResponse>> middlewares = [];

    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public ConquerorContext ConquerorContext { get; } = conquerorContext;

    public QueryTransportType TransportType { get; } = transportType;

    public IQueryPipeline<TQuery, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>
    {
        middlewares.Add(middleware);
        return this;
    }

    public IQueryPipeline<TQuery, TResponse> Use(QueryMiddlewareFn<TQuery, TResponse> middlewareFn)
    {
        return Use(new DelegateQueryMiddleware(middlewareFn));
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

    public QueryPipelineRunner<TQuery, TResponse> Build(ConquerorContext conquerorContext)
    {
        return new(conquerorContext, middlewares);
    }

    private sealed class DelegateQueryMiddleware(QueryMiddlewareFn<TQuery, TResponse> middlewareFn) : IQueryMiddleware<TQuery, TResponse>
    {
        public Task<TResponse> Execute(QueryMiddlewareContext<TQuery, TResponse> ctx) => middlewareFn(ctx);
    }
}
