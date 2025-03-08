using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Publishing;

internal sealed class ParallelBroadcastingStrategy(ParallelEventBroadcastingStrategyConfiguration configuration) : IEventBroadcastingStrategy
{
    public async Task BroadcastEvent(IReadOnlyCollection<EventObserverFn> eventObservers,
                                     IServiceProvider serviceProvider,
                                     object evt,
                                     CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(configuration.MaxDegreeOfParallelism ?? 1_000_000);

        // ReSharper disable once AccessToDisposedClosure (semaphore is disposed only after all executions have finished)
        var potentialExceptions = await Task.WhenAll(eventObservers.Select(o => ExecuteObserver(o, semaphore, evt, cancellationToken))).ConfigureAwait(false);

        var thrownExceptions = potentialExceptions.OfType<Exception>().ToList();

        if (thrownExceptions.Count == 0)
        {
            return;
        }

        // ReSharper disable once MergeIntoPattern (not supported for .NET 6)
        if (thrownExceptions.Count == 1 && thrownExceptions[0] is OperationCanceledException canceledException)
        {
            ExceptionDispatchInfo.Capture(canceledException).Throw();
        }

        if (thrownExceptions.TrueForAll(ex => ex is OperationCanceledException))
        {
            ExceptionDispatchInfo.Capture(thrownExceptions[0]).Throw();
        }

        throw new AggregateException(thrownExceptions);
    }

    private async Task<Exception?> ExecuteObserver<TEvent>(EventObserverFn observer,
                                                           SemaphoreSlim semaphore,
                                                           TEvent evt,
                                                           CancellationToken cancellationToken = default)
        where TEvent : class
    {
        try
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // on cancellation, we want to give each observer the chance to decide what to do about it
            // and therefore we just ignore the exception here (passing the token is still necessary
            // to allow an early exit out of the wait)
        }
        catch (Exception e)
        {
            return e;
        }

        try
        {
            await observer(evt, cancellationToken).ConfigureAwait(false);

            return null;
        }
        catch (Exception e)
        {
            return e;
        }
        finally
        {
            _ = semaphore.Release();
        }
    }
}
