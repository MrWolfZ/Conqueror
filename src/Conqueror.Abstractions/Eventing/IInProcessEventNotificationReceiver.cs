using System;

// ReSharper disable once CheckNamespace
namespace Conqueror;

public interface IInProcessEventNotificationReceiver<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    /// <summary>
    ///     Note that this is the service provider from the global scope, and <i>not</i> the service provider
    ///     from the scope of the publish operation.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    bool IsEnabled { get; }

    void Enable();

    void Disable();
}
