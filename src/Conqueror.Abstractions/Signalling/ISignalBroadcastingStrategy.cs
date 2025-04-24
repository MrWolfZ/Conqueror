using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalBroadcastingStrategy
{
    Task BroadcastSignal<TSignal>(IReadOnlyCollection<SignalHandlerFn<TSignal>> signalHandlerInvocationFns,
                                  IServiceProvider serviceProvider,
                                  TSignal signal,
                                  CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>;
}
