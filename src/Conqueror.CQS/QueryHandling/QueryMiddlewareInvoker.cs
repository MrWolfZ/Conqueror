using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

// these classes belong together
#pragma warning disable SA1402

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryMiddlewareInvoker<TConfiguration> : IQueryMiddlewareInvoker
    {
        public async Task<TResponse> Invoke<TQuery, TResponse>(TQuery query,
                                                               QueryMiddlewareNext<TQuery, TResponse> next,
                                                               Type middlewareType,
                                                               object? middlewareConfiguration,
                                                               IServiceProvider serviceProvider,
                                                               CancellationToken cancellationToken)
            where TQuery : class
        {
            if (middlewareConfiguration is null)
            {
                throw new ArgumentNullException(nameof(middlewareConfiguration));
            }

            var configuration = (TConfiguration)middlewareConfiguration;

            var ctx = new DefaultQueryMiddlewareContext<TQuery, TResponse, TConfiguration>(query, next, configuration, cancellationToken);

            var middleware = (IQueryMiddleware<TConfiguration>)serviceProvider.GetRequiredService(middlewareType);

            return await middleware.Execute(ctx);
        }
    }

    internal sealed class QueryMiddlewareInvoker : IQueryMiddlewareInvoker
    {
        public async Task<TResponse> Invoke<TQuery, TResponse>(TQuery query,
                                                               QueryMiddlewareNext<TQuery, TResponse> next,
                                                               Type middlewareType,
                                                               object? middlewareConfiguration,
                                                               IServiceProvider serviceProvider,
                                                               CancellationToken cancellationToken)
            where TQuery : class
        {
            var ctx = new DefaultQueryMiddlewareContext<TQuery, TResponse>(query, next, cancellationToken);

            var middleware = (IQueryMiddleware)serviceProvider.GetRequiredService(middlewareType);

            return await middleware.Execute(ctx);
        }
    }
}
