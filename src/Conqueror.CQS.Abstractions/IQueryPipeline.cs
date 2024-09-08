using System;

namespace Conqueror;

public interface IQueryPipeline<TQuery, TResponse>
    where TQuery : class
{
    IServiceProvider ServiceProvider { get; }

    IQueryPipeline<TQuery, TResponse> Use<TMiddleware>()
        where TMiddleware : IQueryMiddleware;

    IQueryPipeline<TQuery, TResponse> Use<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IQueryMiddleware<TConfiguration>;

    IQueryPipeline<TQuery, TResponse> Without<TMiddleware>()
        where TMiddleware : IQueryMiddleware;

    IQueryPipeline<TQuery, TResponse> Without<TMiddleware, TConfiguration>()
        where TMiddleware : IQueryMiddleware<TConfiguration>;

    IQueryPipeline<TQuery, TResponse> Configure<TMiddleware, TConfiguration>(TConfiguration configuration)
        where TMiddleware : IQueryMiddleware<TConfiguration>;

    IQueryPipeline<TQuery, TResponse> Configure<TMiddleware, TConfiguration>(Action<TConfiguration> configure)
        where TMiddleware : IQueryMiddleware<TConfiguration>;

    IQueryPipeline<TQuery, TResponse> Configure<TMiddleware, TConfiguration>(Func<TConfiguration, TConfiguration> configure)
        where TMiddleware : IQueryMiddleware<TConfiguration>;
}
