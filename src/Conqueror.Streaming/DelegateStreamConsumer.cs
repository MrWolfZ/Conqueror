namespace Conqueror.Streaming;

internal sealed class DelegateStreamConsumer<TItem>(
    Func<TItem, IServiceProvider, CancellationToken, Task> consumerFn,
    IServiceProvider serviceProvider
) : IStreamConsumer<TItem>
{
    public Task HandleItem(TItem item, CancellationToken cancellationToken = default) =>
        consumerFn(item, serviceProvider, cancellationToken);
}
