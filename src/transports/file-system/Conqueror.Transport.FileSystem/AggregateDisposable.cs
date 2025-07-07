namespace Conqueror.Transport.FileSystem;

internal sealed class AggregateDisposable(int capacity) : IDisposable
{
    private readonly IDisposable?[] disposables = ArrayPool<IDisposable?>.Shared.Rent(capacity);

    private int count;

    public void Add(IDisposable disposable) => disposables[count++] = disposable;

    public void Dispose()
    {
        foreach (var disposable in disposables.OfType<IDisposable>())
        {
            disposable.Dispose();
        }
    }
}
