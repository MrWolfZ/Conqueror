using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface ISignalReceiverHandlerInvoker
{
    Type SignalType { get; }

    Type? HandlerType { get; }

    /// <summary>
    ///     Will throw if the signal is not assignable to the type <see cref="SignalType" />.
    /// </summary>
    Task Invoke(
        object signal,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken);
}

public interface ISignalReceiverHandlerInvoker<out TTypesInjector> : ISignalReceiverHandlerInvoker
    where TTypesInjector : class, ISignalHandlerTypesInjector
{
    TTypesInjector TypesInjector { get; }
}
