namespace Conqueror.Transport.FileSystem;

internal sealed class AggregateAsyncDisposable(int capacity) : IAsyncDisposable
{
    private readonly IAsyncDisposable?[] asyncDisposables = ArrayPool<IAsyncDisposable?>.Shared.Rent(capacity);

    private int count;

    public void Add(IAsyncDisposable disposable) => asyncDisposables[count++] = disposable;

    public async ValueTask DisposeAsync()
    {
        foreach (var asyncDisposable in asyncDisposables.OfType<IAsyncDisposable>())
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}
