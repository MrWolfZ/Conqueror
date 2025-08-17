namespace Conqueror.Signalling;

using System.Runtime.ExceptionServices;
using static SequentialSignalBroadcastingStrategyConfiguration;

internal sealed class SequentialSignalBroadcastingStrategy(
    SequentialSignalBroadcastingStrategyConfiguration configuration
) : ISignalBroadcastingStrategy
{
    public static readonly SequentialSignalBroadcastingStrategy Default = new(new());

    public async Task BroadcastSignal<TSignal>(
        IReadOnlyCollection<SignalHandlerFn<TSignal>> signalHandlerInvocationFns,
        IServiceProvider serviceProvider,
        TSignal signal,
        CancellationToken cancellationToken
    )
        where TSignal : class, ISignal<TSignal>
    {
        var shouldThrowOnFirst = configuration.ExceptionHandling is ExceptionHandlingStrategy.ThrowOnFirstException;
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
            catch (Exception) when (shouldThrowOnFirst && thrownCancellationExceptions.Count is 0)
            {
                throw;
            }
            catch (Exception ex) when (shouldThrowOnFirst)
            {
                throw new AggregateException(new[] { ex }.Concat(thrownCancellationExceptions));
            }
            catch (Exception e)
            {
                thrownExceptions.Add(e);
            }
        }

        if (thrownExceptions.Count is 0)
        {
            if (thrownCancellationExceptions.FirstOrDefault() is { } cancelException)
            {
                ExceptionDispatchInfo.Capture(cancelException).Throw();
            }

            return;
        }

        thrownExceptions.AddRange(thrownCancellationExceptions);

        if (thrownExceptions.Count is 1)
        {
            ExceptionDispatchInfo.Capture(thrownExceptions[0]).Throw();
        }

        throw new AggregateException(thrownExceptions);
    }
}
