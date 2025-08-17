namespace Conqueror.Transport.FileSystem;

internal sealed class DisposableSemaphore(int initialCount = 1, int maxCount = 1) : IDisposable
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "false positive")]
    private SemaphoreSlim? semaphore = new(initialCount, maxCount);

    public void Dispose()
    {
        var s = Interlocked.Exchange(ref semaphore, value: null);
        s?.Dispose();
    }

    public async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(semaphore is null, nameof(DisposableSemaphore));

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new ReleaseDisposable(this);
    }

    private int Release() => semaphore?.Release() ?? 0;

    private sealed class ReleaseDisposable(DisposableSemaphore semaphore) : IDisposable
    {
        public void Dispose()
        {
            var count = semaphore.Release();
            Debug.Assert(count is 0, $"expected the semaphore to have had a value of 0, but it was {count}");
        }
    }
}
