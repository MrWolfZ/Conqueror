using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Signalling;

internal sealed class ParallelSignalBroadcastingStrategy(
    ParallelSignalBroadcastingStrategyConfiguration configuration)
    : ISignalBroadcastingStrategy
{
    public static readonly ParallelSignalBroadcastingStrategy Default = new(new());

    public async Task BroadcastSignal<TSignal>(IReadOnlyCollection<SignalHandlerFn<TSignal>> signalHandlerInvocationFns,
                                               IServiceProvider serviceProvider,
                                               TSignal signal,
                                               CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>
    {
        using var semaphore = new SemaphoreSlim(configuration.MaxDegreeOfParallelism ?? 1_000_000);

        var potentialExceptions = await Task.WhenAll(signalHandlerInvocationFns.Select(ExecuteInvoker)).ConfigureAwait(false);

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

        async Task<Exception?> ExecuteInvoker(SignalHandlerFn<TSignal> invocationFn)
        {
            try
            {
                // ReSharper disable once AccessToDisposedClosure (semaphore is disposed only after all executions have finished)
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
                await invocationFn(signal, serviceProvider, cancellationToken).ConfigureAwait(false);

                return null;
            }
            catch (Exception e)
            {
                return e;
            }
            finally
            {
                // ReSharper disable once AccessToDisposedClosure (semaphore is disposed only after all executions have finished)
                _ = semaphore.Release();
            }
        }
    }
}
