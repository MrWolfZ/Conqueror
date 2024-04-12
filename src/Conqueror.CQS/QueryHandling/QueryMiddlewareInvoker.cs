using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.CQS.QueryHandling;

internal sealed class QueryMiddlewareInvoker<TMiddleware, TConfiguration> : IQueryMiddlewareInvoker
{
    public Type MiddlewareType => typeof(TMiddleware);

    public Task<TResponse> Invoke<TQuery, TResponse>(TQuery query,
                                                     QueryMiddlewareNext<TQuery, TResponse> next,
                                                     object? middlewareConfiguration,
                                                     IServiceProvider serviceProvider,
                                                     IConquerorContext conquerorContext,
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

        var ctx = new DefaultQueryMiddlewareContext<TQuery, TResponse, TConfiguration>(query, next, configuration, serviceProvider, conquerorContext, cancellationToken);

        if (typeof(TConfiguration) == typeof(NullQueryMiddlewareConfiguration))
        {
            var middleware = (IQueryMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));
            return middleware.Execute(ctx);
        }

        var middlewareWithConfiguration = (IQueryMiddleware<TConfiguration>)serviceProvider.GetRequiredService(typeof(TMiddleware));
        return middlewareWithConfiguration.Execute(ctx);
    }
}

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "It is fine for a null-object to be empty.")]
internal sealed record NullQueryMiddlewareConfiguration;
