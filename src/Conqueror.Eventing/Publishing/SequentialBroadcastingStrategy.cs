﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Conqueror.Eventing.Publishing;

internal sealed class SequentialBroadcastingStrategy(SequentialEventBroadcastingStrategyConfiguration configuration) : IEventBroadcastingStrategy
{
    public async Task BroadcastEvent(IReadOnlyCollection<EventObserverFn> eventObservers,
                                     IServiceProvider serviceProvider,
                                     object evt,
                                     CancellationToken cancellationToken)
    {
        var shouldThrowOnFirst = configuration.ExceptionHandling == SequentialEventBroadcastingStrategyConfiguration.ExceptionHandlingStrategy.ThrowOnFirstException;
        var thrownExceptions = new List<Exception>();
        var thrownCancellationExceptions = new List<Exception>();

        foreach (var observer in eventObservers)
        {
            try
            {
                await observer(evt, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                thrownCancellationExceptions.Add(e);
            }
            catch (Exception e)
            {
                if (shouldThrowOnFirst)
                {
                    if (thrownCancellationExceptions.Any())
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
