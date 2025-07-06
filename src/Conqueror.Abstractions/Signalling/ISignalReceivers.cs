using System;
using System.Collections.Generic;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalReceivers
{
    IServiceProvider ServiceProvider { get; }

    ReceiverExecutionHandle RunReceivers<TTypesInjector, TReceiver>(
        ISignalReceiverFactory<TTypesInjector, TReceiver> receiverFactory,
        ISignalReceiverRunner<TReceiver> receiverRunner,
        CancellationToken cancellationToken)
        where TTypesInjector : class, ISignalHandlerTypesInjector
        where TReceiver : class;

    ReceiverExecutionHandle RunReceiver<THandler, TTypesInjector, TReceiver>(
        ISignalReceiverFactory<TTypesInjector, TReceiver> receiverFactory,
        ISignalReceiverRunner<TReceiver> receiverRunner,
        CancellationToken cancellationToken)
        where TTypesInjector : class, ISignalHandlerTypesInjector
        where TReceiver : class;

    ReceiverExecutionHandle CombineExecutions(IReadOnlyCollection<ReceiverExecutionHandle> executionHandles);
}
