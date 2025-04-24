using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IMessageReceiver<out TReceiver>
    where TReceiver : class, IMessageReceiver<TReceiver>
{
    Type MessageType { get; }

    /// <summary>
    ///     Note that this is (usually) the service provider from the global scope,
    ///     and <i>not</i> the service provider from the scope of the send operation.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    bool IsEnabled { get; }

    TReceiver Enable();

    TReceiver Disable();
}
