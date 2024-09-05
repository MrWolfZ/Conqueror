using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal abstract class StreamConsumerGeneratedProxyBase<TItem> : IStreamConsumer<TItem>
{
    private readonly IStreamConsumer<TItem> target;

    protected StreamConsumerGeneratedProxyBase(IStreamConsumer<TItem> target)
    {
        this.target = target;
    }

    public Task HandleItem(TItem item, CancellationToken cancellationToken = default)
    {
        return target.HandleItem(item, cancellationToken);
    }
}
