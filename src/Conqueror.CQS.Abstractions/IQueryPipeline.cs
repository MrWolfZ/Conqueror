using System;

namespace Conqueror;

public interface IQueryPipeline<TQuery, TResponse>
    where TQuery : class
{
    IServiceProvider ServiceProvider { get; }

    IQueryPipeline<TQuery, TResponse> Use<TMiddleware>(TMiddleware middleware)
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>;

    IQueryPipeline<TQuery, TResponse> Without<TMiddleware>()
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>;

    IQueryPipeline<TQuery, TResponse> Configure<TMiddleware>(Action<TMiddleware> configure)
        where TMiddleware : IQueryMiddleware<TQuery, TResponse>;
}
