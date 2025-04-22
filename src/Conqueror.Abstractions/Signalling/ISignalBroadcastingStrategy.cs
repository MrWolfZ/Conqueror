using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalBroadcastingStrategy
{
    Task BroadcastSignal<TSignal>(IReadOnlyCollection<ISignalReceiverHandlerInvoker> signalHandlerInvokers,
                                  IServiceProvider serviceProvider,
                                  TSignal signal,
                                  string transportTypeName,
                                  CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>;
}
