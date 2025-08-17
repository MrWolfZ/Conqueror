namespace Conqueror.Streaming;

internal abstract class StreamConsumerGeneratedProxyBase<TItem>(IStreamConsumer<TItem> target) : IStreamConsumer<TItem>
{
    public Task HandleItem(TItem item, CancellationToken cancellationToken = default) =>
        target.HandleItem(item, cancellationToken);
}
