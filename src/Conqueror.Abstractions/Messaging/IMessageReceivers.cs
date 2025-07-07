using System;
using System.Collections.Generic;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageReceivers
{
    IServiceProvider ServiceProvider { get; }

    ReceiverExecutionHandle RunReceivers<TTypesInjector, TReceiver>(
        IMessageReceiverFactory<TTypesInjector, TReceiver> receiverFactory,
        IMessageReceiverRunner<TReceiver> receiverRunner,
        CancellationToken cancellationToken)
        where TTypesInjector : class, IMessageHandlerTypesInjector
        where TReceiver : class;

    ReceiverExecutionHandle RunReceiver<THandler, TTypesInjector, TReceiver>(
        IMessageReceiverFactory<TTypesInjector, TReceiver> receiverFactory,
        IMessageReceiverRunner<TReceiver> receiverRunner,
        CancellationToken cancellationToken)
        where TTypesInjector : class, IMessageHandlerTypesInjector
        where TReceiver : class;

    ReceiverExecutionHandle CombineExecutions(IReadOnlyCollection<ReceiverExecutionHandle> executionHandles);
}
