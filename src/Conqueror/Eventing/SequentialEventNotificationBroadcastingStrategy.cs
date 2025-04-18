using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using static Conqueror.Eventing.SequentialEventNotificationBroadcastingStrategyConfiguration;

namespace Conqueror.Eventing;

internal sealed class SequentialEventNotificationBroadcastingStrategy(
    SequentialEventNotificationBroadcastingStrategyConfiguration configuration)
    : IEventNotificationBroadcastingStrategy
{
    public static readonly SequentialEventNotificationBroadcastingStrategy Default = new(new());

    public async Task BroadcastEventNotification<TEventNotification>(IReadOnlyCollection<IEventNotificationReceiverHandlerInvoker> eventNotificationHandlerInvokers,
                                                                     IServiceProvider serviceProvider,
                                                                     TEventNotification notification,
                                                                     string transportTypeName,
                                                                     CancellationToken cancellationToken)
        where TEventNotification : class, IEventNotification<TEventNotification>
    {
        var shouldThrowOnFirst = configuration.ExceptionHandling == ExceptionHandlingStrategy.ThrowOnFirstException;
        var thrownExceptions = new List<Exception>();
        var thrownCancellationExceptions = new List<Exception>();

        foreach (var invoker in eventNotificationHandlerInvokers)
        {
            try
            {
                await invoker.Invoke(serviceProvider, notification, transportTypeName, cancellationToken).ConfigureAwait(false);
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
