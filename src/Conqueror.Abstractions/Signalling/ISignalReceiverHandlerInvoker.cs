using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalReceiverHandlerInvoker
{
    Task Invoke<TSignal>(
        IServiceProvider serviceProvider,
        TSignal signal,
        string transportTypeName,
        CancellationToken cancellationToken)
        where TSignal : class, ISignal<TSignal>;
}

public interface ISignalReceiverHandlerInvoker<out TTypesInjector> : ISignalReceiverHandlerInvoker
    where TTypesInjector : class, ISignalHandlerTypesInjector
{
    Type SignalType { get; }

    Type? HandlerType { get; }

    TTypesInjector TypesInjector { get; }
}
