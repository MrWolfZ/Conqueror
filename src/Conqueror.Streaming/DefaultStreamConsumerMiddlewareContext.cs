using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal delegate Task StreamConsumerMiddlewareNext<in TItem>(TItem request, CancellationToken cancellationToken);

internal sealed class DefaultStreamConsumerMiddlewareContext<TItem, TConfiguration>(
    TItem item,
    StreamConsumerMiddlewareNext<TItem> next,
    TConfiguration configuration,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    CancellationToken cancellationToken)
    : StreamConsumerMiddlewareContext<TItem, TConfiguration>
{
    public override TItem Item { get; } = item;

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override ConquerorContext ConquerorContext { get; } = conquerorContext;

    public override TConfiguration Configuration { get; } = configuration;

    public override Task Next(TItem item, CancellationToken cancellationToken) => next(item, cancellationToken);
}
