using System.Diagnostics.CodeAnalysis;

namespace Conqueror.Transport.FileSystem;

internal sealed class DisposableSemaphore(int initialCount = 1, int maxCount = 1) : IDisposable
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "false positive")]
    private SemaphoreSlim? semaphore = new(initialCount, maxCount);

    public async Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
    {
        if (semaphore is null)
        {
            throw new ObjectDisposedException(nameof(DisposableSemaphore));
        }

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        return new ReleaseDisposable(this);
    }

    public void Dispose()
    {
        var s = Interlocked.Exchange(ref semaphore, null);
        s?.Dispose();
    }

    private int Release() => semaphore?.Release() ?? 0;

    private sealed class ReleaseDisposable(DisposableSemaphore semaphore) : IDisposable
    {
        public void Dispose()
        {
            var count = semaphore.Release();
            Debug.Assert(count == 0, $"expected the semaphore to have had a value of 0, but it was {count}");
        }
    }
}
