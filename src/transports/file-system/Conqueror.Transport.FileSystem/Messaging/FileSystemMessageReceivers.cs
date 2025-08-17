namespace Conqueror.Transport.FileSystem.Messaging;

internal sealed class FileSystemMessageReceivers : IFileSystemMessageReceivers
{
    public ReceiverExecutionHandle RunReceivers(IMessageReceivers receivers, CancellationToken cancellationToken)
    {
        return receivers.RunReceivers(
            receivers.ServiceProvider.GetRequiredService<FileSystemMessageReceiverFactory>(),
            receivers.ServiceProvider.GetRequiredService<FileSystemMessageReceiverRunner>(),
            cancellationToken
        );
    }

    public ReceiverExecutionHandle RunReceiver<THandler>(
        IMessageReceivers receivers,
        CancellationToken cancellationToken
    )
        where THandler : class, IFileSystemMessageHandler, IMessageHandlerWithSourceGeneration
    {
        return receivers.RunReceiver<THandler, IFileSystemMessageHandlerTypesInjector, FileSystemMessageReceiver>(
            receivers.ServiceProvider.GetRequiredService<FileSystemMessageReceiverFactory>(),
            receivers.ServiceProvider.GetRequiredService<FileSystemMessageReceiverRunner>(),
            cancellationToken
        );
    }
}
