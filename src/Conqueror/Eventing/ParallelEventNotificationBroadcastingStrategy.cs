using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing;

internal sealed class ParallelEventNotificationBroadcastingStrategy(
    ParallelEventNotificationBroadcastingStrategyConfiguration configuration)
    : IEventNotificationBroadcastingStrategy
{
    public static readonly ParallelEventNotificationBroadcastingStrategy Default = new(new());

    public async Task BroadcastEventNotification<TEventNotification>(IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker> eventNotificationHandlerInvokers,
                                                                     IServiceProvider serviceProvider,
                                                                     TEventNotification notification,
                                                                     string transportTypeName,
                                                                     CancellationToken cancellationToken)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        using var semaphore = new SemaphoreSlim(configuration.MaxDegreeOfParallelism ?? 1_000_000);

        var potentialExceptions = await Task.WhenAll(eventNotificationHandlerInvokers.Select(ExecuteInvoker)).ConfigureAwait(false);

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

        async Task<Exception?> ExecuteInvoker(IEventNotificationReceiverHandlerInvoker invoker)
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
                await invoker.Invoke(serviceProvider, notification, transportTypeName, cancellationToken).ConfigureAwait(false);

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
