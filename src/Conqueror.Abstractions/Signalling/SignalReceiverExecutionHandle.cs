using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public sealed class SignalReceiverExecutionHandle : IAsyncDisposable
{
    private Action? onDispose;
    private CancellationTokenSource? cancellationTokenSource;
    private IReadOnlyCollection<SignalReceiverExecutionHandle>? innerHandles;

    public SignalReceiverExecutionHandle(
        Task initialConnectionTask,
        Task completionTask,
        CancellationTokenSource? cancellationTokenSource,
        Action? onDispose)
    {
        InitialConnectionTask = initialConnectionTask;
        CompletionTask = completionTask;
        this.cancellationTokenSource = cancellationTokenSource;
        this.onDispose = onDispose;
    }

    public SignalReceiverExecutionHandle(IReadOnlyCollection<SignalReceiverExecutionHandle> handles)
    {
        InitialConnectionTask = WhenAll(handles.Select(r => r.InitialConnectionTask));
        CompletionTask = WhenAll(handles.Select(r => r.CompletionTask));
        innerHandles = handles;
    }

    public Task InitialConnectionTask { get; }

    public Task CompletionTask { get; }

    public IReadOnlyCollection<SignalReceiverExecutionHandle>? InnerHandles => innerHandles;

    public async ValueTask DisposeAsync()
    {
        var cts = Interlocked.Exchange(ref cancellationTokenSource, null);

        if (cts is not null)
        {
            await cts.CancelAsync().ConfigureAwait(false);
            cts.Dispose();
        }

        var handles = Interlocked.Exchange(ref innerHandles, null);

        if (handles is not null)
        {
            await Task.WhenAll(handles.Select(DisposeSingle)).ConfigureAwait(false);
        }

        var onD = Interlocked.Exchange(ref onDispose, null);
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

    private static async Task DisposeSingle(SignalReceiverExecutionHandle handle)
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
