using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IInProcessMessageReceiver
{
    Type MessageType { get; }

    /// <summary>
    ///     Note that this is the service provider from the global scope.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    bool IsEnabled { get; }

    void Disable();

    IInProcessMessageReceiver ConfigureOnEveryMessage();
}
