using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Streaming;

internal abstract class StreamProducerGeneratedProxyBase<TRequest, TItem> : IStreamProducer<TRequest, TItem>
    where TRequest : class
{
    private readonly IStreamProducer<TRequest, TItem> target;

    protected StreamProducerGeneratedProxyBase(IStreamProducer<TRequest, TItem> target)
    {
        this.target = target;
    }

    public IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken = default)
    {
        return target.ExecuteRequest(request, cancellationToken);
    }
}
