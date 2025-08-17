namespace Conqueror.Signalling;

internal interface ISignalHandlerInvoker
{
    Task Invoke(
        object signal,
        IServiceProvider serviceProvider,
        string transportTypeName,
        CancellationToken cancellationToken
    );
}
