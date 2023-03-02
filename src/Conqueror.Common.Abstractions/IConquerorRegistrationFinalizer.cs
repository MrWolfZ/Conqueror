namespace Conqueror;

/// <summary>
///     Registration finalizers are executed before the service provider is built and have
///     the chance to modify the services.
/// </summary>
public interface IConquerorRegistrationFinalizer
{
    /// <summary>
    ///     The phase in which the finalizer should be called. This is useful for example to
    ///     execute a transport package finalizer after the base library finalizer has run.
    ///     A lower number means the finalizer is executed first. This value should default
    ///     to 1.
    /// </summary>
    int ExecutionPhase { get; }

    /// <summary>
    ///     Execute this registration finalizer.
    /// </summary>
    void Execute();
}
