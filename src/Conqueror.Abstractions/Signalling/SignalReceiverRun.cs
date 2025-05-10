using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public sealed class SignalReceiverRun : IAsyncDisposable
{
    private CancellationTokenSource? cancellationTokenSource;
    private IReadOnlyCollection<SignalReceiverRun>? innerRuns;

    [SuppressMessage("ReSharper", "ConvertToPrimaryConstructor", Justification = "false positive")]
    public SignalReceiverRun(Task completionTask, CancellationTokenSource? cancellationTokenSource)
    {
        CompletionTask = completionTask;
        this.cancellationTokenSource = cancellationTokenSource;
    }

    public SignalReceiverRun(IReadOnlyCollection<SignalReceiverRun> runs)
    {
        CompletionTask = RunAll(runs);
        innerRuns = runs;
    }

    public Task CompletionTask { get; }

    public IReadOnlyCollection<SignalReceiverRun>? InnerRuns => innerRuns;

    public async ValueTask DisposeAsync()
    {
        var cts = Interlocked.Exchange(ref cancellationTokenSource, null);

        if (cts is not null)
        {
            await cts.CancelAsync().ConfigureAwait(false);
            cts.Dispose();
        }

        var runs = Interlocked.Exchange(ref innerRuns, null);

        if (runs is not null)
        {
            await Task.WhenAll(runs.Select(DisposeSingle)).ConfigureAwait(false);
        }
    }

    private static async Task RunAll(IReadOnlyCollection<SignalReceiverRun> runs)
    {
        var task = Task.WhenAll(runs.Select(r => r.CompletionTask));

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

    private static async Task DisposeSingle(SignalReceiverRun run)
    {
        try
        {
            await run.DisposeAsync().ConfigureAwait(false);
            await run.CompletionTask.ConfigureAwait(false);
        }
        catch
        {
            // during disposal we don't care about exceptions
        }
    }
}
