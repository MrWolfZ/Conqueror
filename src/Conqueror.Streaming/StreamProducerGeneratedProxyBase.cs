using System.Collections.Generic;
using System.Threading;

namespace Conqueror.Streaming;

internal abstract class StreamProducerGeneratedProxyBase<TRequest, TItem>(IStreamProducer<TRequest, TItem> target) : IStreamProducer<TRequest, TItem>
    where TRequest : class
{
    public IAsyncEnumerable<TItem> ExecuteRequest(TRequest request, CancellationToken cancellationToken = default)
    {
        return target.ExecuteRequest(request, cancellationToken);
    }
}
