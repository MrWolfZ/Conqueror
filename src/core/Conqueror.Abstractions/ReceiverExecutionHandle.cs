namespace Conqueror;

using System.Runtime.ExceptionServices;

public sealed class ReceiverExecutionHandle : IAsyncDisposable
{
    private CancellationTokenSource? cancellationTokenSource;
    private IReadOnlyCollection<ReceiverExecutionHandle>? innerHandles;
    private Action? onDispose;

    public ReceiverExecutionHandle(
        Task initialConnectionTask,
        Task completionTask,
        CancellationTokenSource? cancellationTokenSource,
        Action? onDispose
    )
    {
        InitialConnectionTask = initialConnectionTask;
        CompletionTask = completionTask;
        this.cancellationTokenSource = cancellationTokenSource;
        this.onDispose = onDispose;
    }

    public ReceiverExecutionHandle(IReadOnlyCollection<ReceiverExecutionHandle> handles)
    {
        InitialConnectionTask = WhenAll(handles.Select(r => r.InitialConnectionTask));
        CompletionTask = WhenAll(handles.Select(r => r.CompletionTask));
        innerHandles = handles;
    }

    public Task InitialConnectionTask { get; }

    public Task CompletionTask { get; }

    public IReadOnlyCollection<ReceiverExecutionHandle>? InnerHandles => innerHandles;

    public async ValueTask DisposeAsync()
    {
        var cts = Interlocked.Exchange(ref cancellationTokenSource, value: null);

        if (cts is not null)
        {
            await cts.CancelAsync().ConfigureAwait(false);
            cts.Dispose();
        }

        var handles = Interlocked.Exchange(ref innerHandles, value: null);

        if (handles is not null)
        {
            await Task.WhenAll(handles.Select(DisposeSingle)).ConfigureAwait(false);
        }

        var onD = Interlocked.Exchange(ref onDispose, value: null);
        onD?.Invoke();
    }

    private static async Task WhenAll(IEnumerable<Task> tasks)
    {
        var task = Task.WhenAll(tasks);

        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // ensure that the AggregateException is not unwrapped
            if (task.Exception is { InnerExceptions.Count: > 1 })
            {
                ExceptionDispatchInfo.Capture(task.Exception).Throw();
            }

            throw;
        }
    }

    private static async Task DisposeSingle(ReceiverExecutionHandle handle)
    {
        try
        {
            await handle.DisposeAsync().ConfigureAwait(false);
            await handle.CompletionTask.ConfigureAwait(false);
        }
        catch
        {
            // during disposal, we don't care about exceptions
        }
    }
}
