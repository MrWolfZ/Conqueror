using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalReceiverFactory<in TTypesInjector, out TReceiver>
    where TTypesInjector : class, ISignalHandlerTypesInjector
    where TReceiver : class
{
    string TransportTypeName { get; }

    TReceiver? CreateReceiverForHandlerType(
        Type? handlerType,
        IReadOnlyCollection<ISignalReceiverHandlerInvoker<TTypesInjector>> invokers,
        TTypesInjector typesInjector);
}
