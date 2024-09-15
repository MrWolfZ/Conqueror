using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Streaming;

internal delegate IAsyncEnumerable<TItem> StreamProducerMiddlewareNext<in TRequest, out TItem>(TRequest request, CancellationToken cancellationToken);

internal sealed class DefaultStreamProducerMiddlewareContext<TRequest, TItem, TConfiguration>(
    TRequest request,
    StreamProducerMiddlewareNext<TRequest, TItem> next,
    TConfiguration configuration,
    IServiceProvider serviceProvider,
    ConquerorContext conquerorContext,
    CancellationToken cancellationToken)
    : StreamProducerMiddlewareContext<TRequest, TItem, TConfiguration>
    where TRequest : class
{
    public override TRequest Request { get; } = request;

    public override CancellationToken CancellationToken { get; } = cancellationToken;

    public override IServiceProvider ServiceProvider { get; } = serviceProvider;

    public override ConquerorContext ConquerorContext { get; } = conquerorContext;

    public override TConfiguration Configuration { get; } = configuration;

    public override IAsyncEnumerable<TItem> Next(TRequest request, CancellationToken cancellationToken) => next(request, cancellationToken);
}
