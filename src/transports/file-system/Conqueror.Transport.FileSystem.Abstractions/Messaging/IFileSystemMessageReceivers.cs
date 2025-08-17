namespace Conqueror;

public interface IFileSystemMessageReceivers
{
    ReceiverExecutionHandle RunReceivers(IMessageReceivers receivers, CancellationToken cancellationToken);

    ReceiverExecutionHandle RunReceiver<THandler>(IMessageReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IFileSystemMessageHandler, IMessageHandlerWithSourceGeneration;
}
