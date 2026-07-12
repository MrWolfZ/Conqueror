namespace Conqueror;

public interface IInProcessIteratorServer
{
    Type IteratorType { get; }

    /// <summary>
    ///     Note that this is the service provider from the global scope.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    bool IsEnabled { get; }

    void Disable();

    void ConfigureOnEveryIteration();
}
