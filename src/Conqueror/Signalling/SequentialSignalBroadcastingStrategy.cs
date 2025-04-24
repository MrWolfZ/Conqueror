using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using static Conqueror.Signalling.SequentialSignalBroadcastingStrategyConfiguration;

namespace Conqueror.Signalling;

internal sealed class SequentialSignalBroadcastingStrategy(
    SequentialSignalBroadcastingStrategyConfiguration configuration)
    : ISignalBroadcastingStrategy
{
    public static readonly SequentialSignalBroadcastingStrategy Default = new(new());

    public async Task BroadcastSignal<TSignal>(IReadOnlyCollection<SignalHandlerFn<TSignal>> signalHandlerInvocationFns,
                                               IServiceProvider serviceProvider,
                                               TSignal signal,
                                               CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>
    {
        var shouldThrowOnFirst = configuration.ExceptionHandling == ExceptionHandlingStrategy.ThrowOnFirstException;
        var thrownExceptions = new List<Exception>();
        var thrownCancellationExceptions = new List<Exception>();

        foreach (var invocationFn in signalHandlerInvocationFns)
        {
            try
            {
                await invocationFn(signal, serviceProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                thrownCancellationExceptions.Add(e);
            }
            catch (Exception e)
            {
                if (shouldThrowOnFirst)
                {
                    if (thrownCancellationExceptions.Count > 0)
                    {
                        throw new AggregateException(new[] { e }.Concat(thrownCancellationExceptions));
                    }

                    throw;
                }

                thrownExceptions.Add(e);
            }
        }

        if (thrownExceptions.Count == 0)
        {
            if (thrownCancellationExceptions.FirstOrDefault() is { } cancelException)
            {
                ExceptionDispatchInfo.Capture(cancelException).Throw();
            }

            return;
        }

        thrownExceptions.AddRange(thrownCancellationExceptions);

        if (thrownExceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(thrownExceptions[0]).Throw();
        }

        throw new AggregateException(thrownExceptions);
    }
}
