using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class StreamingRequestMiddlewareInvoker<TMiddleware, TConfiguration> : IStreamingRequestMiddlewareInvoker
{
    public Type MiddlewareType => typeof(TMiddleware);

    public IAsyncEnumerable<TItem> Invoke<TRequest, TItem>(TRequest request,
                                                           StreamingRequestMiddlewareNext<TRequest, TItem> next,
                                                           object? middlewareConfiguration,
                                                           IServiceProvider serviceProvider,
                                                           IConquerorContext conquerorContext,
                                                           CancellationToken cancellationToken)
        where TRequest : class
    {
        if (typeof(TConfiguration) == typeof(NullStreamingRequestMiddlewareConfiguration))
        {
            middlewareConfiguration = new NullStreamingRequestMiddlewareConfiguration();
        }

        if (middlewareConfiguration is null)
        {
            throw new ArgumentNullException(nameof(middlewareConfiguration));
        }

        var configuration = (TConfiguration)middlewareConfiguration;

        var ctx = new DefaultStreamingRequestMiddlewareContext<TRequest, TItem, TConfiguration>(request, next, configuration, serviceProvider, conquerorContext, cancellationToken);

        if (typeof(TConfiguration) == typeof(NullStreamingRequestMiddlewareConfiguration))
        {
            var middleware = (IStreamingRequestMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));
            return middleware.Execute(ctx);
        }

        var middlewareWithConfiguration = (IStreamingRequestMiddleware<TConfiguration>)serviceProvider.GetRequiredService(typeof(TMiddleware));
        return middlewareWithConfiguration.Execute(ctx);
    }
}

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "It is fine for a null-object to be empty.")]
internal sealed record NullStreamingRequestMiddlewareConfiguration;
