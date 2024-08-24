using System;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Streaming;

internal sealed class DelegateStreamConsumer<TItem>(
    Func<TItem, IServiceProvider, CancellationToken, Task> consumerFn,
    IServiceProvider serviceProvider)
    : IStreamConsumer<TItem>
{
    public Task HandleItem(TItem item, CancellationToken cancellationToken = default)
    {
        return consumerFn(item, serviceProvider, cancellationToken);
    }
}
