namespace Conqueror;

public interface IHttpWebSocketsSignalReceivers
{
    ReceiverExecutionHandle RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken);

    ReceiverExecutionHandle RunReceiver<THandler>(ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IHttpWebSocketsSignalHandler, ISignalHandlerWithSourceGeneration;
}
