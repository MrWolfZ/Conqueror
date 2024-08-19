using System;
using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Streaming;

internal delegate IAsyncEnumerable<TItem> StreamProducerMiddlewareNext<in TRequest, out TItem>(TRequest request, CancellationToken cancellationToken);

internal sealed class DefaultStreamProducerMiddlewareContext<TRequest, TItem, TConfiguration> : StreamProducerMiddlewareContext<TRequest, TItem, TConfiguration>
    where TRequest : class
{
    private readonly StreamProducerMiddlewareNext<TRequest, TItem> next;

    public DefaultStreamProducerMiddlewareContext(TRequest request,
                                                  StreamProducerMiddlewareNext<TRequest, TItem> next,
                                                  TConfiguration configuration,
                                                  IServiceProvider serviceProvider,
                                                  IConquerorContext conquerorContext,
                                                  CancellationToken cancellationToken)
    {
        this.next = next;
        Request = request;
        CancellationToken = cancellationToken;
        ServiceProvider = serviceProvider;
        ConquerorContext = conquerorContext;
        Configuration = configuration;
    }

    public override TRequest Request { get; }

    public override CancellationToken CancellationToken { get; }

    public override IServiceProvider ServiceProvider { get; }

    public override IConquerorContext ConquerorContext { get; }

    public override TConfiguration Configuration { get; }

    public override IAsyncEnumerable<TItem> Next(TRequest request, CancellationToken cancellationToken) => next(request, cancellationToken);
}
