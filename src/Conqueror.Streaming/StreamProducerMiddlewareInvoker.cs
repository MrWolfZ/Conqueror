using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class StreamProducerMiddlewareInvoker<TMiddleware, TConfiguration> : IStreamProducerMiddlewareInvoker
{
    public Type MiddlewareType => typeof(TMiddleware);

    public IAsyncEnumerable<TItem> Invoke<TRequest, TItem>(TRequest request,
                                                           StreamProducerMiddlewareNext<TRequest, TItem> next,
                                                           object? middlewareConfiguration,
                                                           IServiceProvider serviceProvider,
                                                           ConquerorContext conquerorContext,
                                                           CancellationToken cancellationToken)
        where TRequest : class
    {
        if (typeof(TConfiguration) == typeof(NullStreamProducerMiddlewareConfiguration))
        {
            middlewareConfiguration = new NullStreamProducerMiddlewareConfiguration();
        }

        if (middlewareConfiguration is null)
        {
            throw new ArgumentNullException(nameof(middlewareConfiguration));
        }

        var configuration = (TConfiguration)middlewareConfiguration;

        var ctx = new DefaultStreamProducerMiddlewareContext<TRequest, TItem, TConfiguration>(request, next, configuration, serviceProvider, conquerorContext, cancellationToken);

        if (typeof(TConfiguration) == typeof(NullStreamProducerMiddlewareConfiguration))
        {
            var middleware = (IStreamProducerMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));
            return middleware.Execute(ctx);
        }

        var middlewareWithConfiguration = (IStreamProducerMiddleware<TConfiguration>)serviceProvider.GetRequiredService(typeof(TMiddleware));
        return middlewareWithConfiguration.Execute(ctx);
    }
}

internal sealed record NullStreamProducerMiddlewareConfiguration;
