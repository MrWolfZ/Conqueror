namespace Conqueror.Transport.FileSystem;

using System.Buffers;

internal sealed class AggregateAsyncDisposable(int capacity) : IAsyncDisposable
{
    private readonly IAsyncDisposable?[] asyncDisposables = ArrayPool<IAsyncDisposable?>.Shared.Rent(capacity);

    private int count;

    public async ValueTask DisposeAsync()
    {
        foreach (var asyncDisposable in asyncDisposables.OfType<IAsyncDisposable>())
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
    }

    public void Add(IAsyncDisposable disposable) => asyncDisposables[count++] = disposable;
}
