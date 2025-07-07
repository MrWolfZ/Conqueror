using System.Threading;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class FileSystemMessageReceiversExtensions
{
    public static ReceiverExecutionHandle RunFileSystemMessageReceivers(this IMessageReceivers receivers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receivers);

        if (receivers.ServiceProvider.GetService(typeof(IFileSystemMessageReceivers)) is not IFileSystemMessageReceivers fileSystemReceivers)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IFileSystemMessageReceivers)}'; did you forget to add the Conqueror file system services to the service collection?");
        }

        return fileSystemReceivers.RunReceivers(receivers, cancellationToken);
    }

    public static ReceiverExecutionHandle RunFileSystemMessageReceiver<THandler>(this IMessageReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IFileSystemMessageHandler, IMessageHandlerWithSourceGeneration
    {
        ArgumentNullException.ThrowIfNull(receivers);

        if (receivers.ServiceProvider.GetService(typeof(IFileSystemMessageReceivers)) is not IFileSystemMessageReceivers fileSystemReceivers)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IFileSystemMessageReceivers)}'; did you forget to add the Conqueror file system services to the service collection?");
        }

        return fileSystemReceivers.RunReceiver<THandler>(receivers, cancellationToken);
    }
}
