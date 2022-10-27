﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling
{
    internal sealed class QueryMiddlewareInvoker<TMiddleware, TConfiguration> : IQueryMiddlewareInvoker
    {
        public Type MiddlewareType => typeof(TMiddleware);

        public async Task<TResponse> Invoke<TQuery, TResponse>(TQuery query,
                                                               QueryMiddlewareNext<TQuery, TResponse> next,
                                                               object? middlewareConfiguration,
                                                               IServiceProvider serviceProvider,
                                                               CancellationToken cancellationToken)
            where TQuery : class
        {
            if (typeof(TConfiguration) == typeof(NullQueryMiddlewareConfiguration))
            {
                middlewareConfiguration = new NullQueryMiddlewareConfiguration();
            }

            if (middlewareConfiguration is null)
            {
                throw new ArgumentNullException(nameof(middlewareConfiguration));
            }

            var configuration = (TConfiguration)middlewareConfiguration;

            var ctx = new DefaultQueryMiddlewareContext<TQuery, TResponse, TConfiguration>(query, next, configuration, cancellationToken);

            if (typeof(TConfiguration) == typeof(NullQueryMiddlewareConfiguration))
            {
                var middleware = (IQueryMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));
                return await middleware.Execute(ctx);
            }

            var middlewareWithConfiguration = (IQueryMiddleware<TConfiguration>)serviceProvider.GetRequiredService(typeof(TMiddleware));
            return await middlewareWithConfiguration.Execute(ctx);
        }
    }

    internal sealed record NullQueryMiddlewareConfiguration;
}
