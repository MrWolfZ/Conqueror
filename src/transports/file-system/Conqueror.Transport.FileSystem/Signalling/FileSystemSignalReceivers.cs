namespace Conqueror.Transport.FileSystem.Signalling;

internal sealed class FileSystemSignalReceivers : IFileSystemSignalReceivers
{
    public ReceiverExecutionHandle RunReceivers(ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        return receivers.RunReceivers(
            receivers.ServiceProvider.GetRequiredService<FileSystemSignalReceiverFactory>(),
            receivers.ServiceProvider.GetRequiredService<FileSystemSignalReceiverRunner>(),
            cancellationToken
        );
    }

    public ReceiverExecutionHandle RunReceiver<THandler>(
        ISignalReceivers receivers,
        CancellationToken cancellationToken
    )
        where THandler : class, IFileSystemSignalHandler, ISignalHandlerWithSourceGeneration
    {
        return receivers.RunReceiver<THandler, IFileSystemSignalHandlerTypesInjector, FileSystemSignalReceiver>(
            receivers.ServiceProvider.GetRequiredService<FileSystemSignalReceiverFactory>(),
            receivers.ServiceProvider.GetRequiredService<FileSystemSignalReceiverRunner>(),
            cancellationToken
        );
    }
}
