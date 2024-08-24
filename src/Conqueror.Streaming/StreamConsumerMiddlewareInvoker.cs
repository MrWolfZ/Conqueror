using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Conqueror.Streaming;

internal sealed class StreamConsumerMiddlewareInvoker<TMiddleware, TConfiguration> : IStreamConsumerMiddlewareInvoker
{
    public Type MiddlewareType => typeof(TMiddleware);

    public Task Invoke<TItem>(TItem item,
                              StreamConsumerMiddlewareNext<TItem> next,
                              object? middlewareConfiguration,
                              IServiceProvider serviceProvider,
                              IConquerorContext conquerorContext,
                              CancellationToken cancellationToken)
    {
        if (typeof(TConfiguration) == typeof(NullStreamConsumerMiddlewareConfiguration))
        {
            middlewareConfiguration = new NullStreamConsumerMiddlewareConfiguration();
        }

        if (middlewareConfiguration is null)
        {
            throw new ArgumentNullException(nameof(middlewareConfiguration));
        }

        var configuration = (TConfiguration)middlewareConfiguration;

        var ctx = new DefaultStreamConsumerMiddlewareContext<TItem, TConfiguration>(item, next, configuration, serviceProvider, conquerorContext, cancellationToken);

        if (typeof(TConfiguration) == typeof(NullStreamConsumerMiddlewareConfiguration))
        {
            var middleware = (IStreamConsumerMiddleware)serviceProvider.GetRequiredService(typeof(TMiddleware));
            return middleware.Execute(ctx);
        }

        var middlewareWithConfiguration = (IStreamConsumerMiddleware<TConfiguration>)serviceProvider.GetRequiredService(typeof(TMiddleware));
        return middlewareWithConfiguration.Execute(ctx);
    }
}

[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty", Justification = "It is fine for a null-object to be empty.")]
internal sealed record NullStreamConsumerMiddlewareConfiguration;
