using System.Threading;

// ReSharper disable once CheckNamespace (we want these extensions to be accessible from client registration code without an extra import)
namespace Conqueror;

public static class FileSystemSignalReceiversExtensions
{
    public static ReceiverExecutionHandle RunFileSystemSignalReceivers(this ISignalReceivers receivers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(receivers);

        if (receivers.ServiceProvider.GetService(typeof(IFileSystemSignalReceivers)) is not IFileSystemSignalReceivers fileSystemReceivers)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IFileSystemSignalReceivers)}'; did you forget to add the Conqueror file system services to the service collection?");
        }

        return fileSystemReceivers.RunReceivers(receivers, cancellationToken);
    }

    public static ReceiverExecutionHandle RunFileSystemSignalReceiver<THandler>(this ISignalReceivers receivers, CancellationToken cancellationToken)
        where THandler : class, IFileSystemSignalHandler, ISignalHandlerWithSourceGeneration
    {
        ArgumentNullException.ThrowIfNull(receivers);

        if (receivers.ServiceProvider.GetService(typeof(IFileSystemSignalReceivers)) is not IFileSystemSignalReceivers fileSystemReceivers)
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IFileSystemSignalReceivers)}'; did you forget to add the Conqueror file system services to the service collection?");
        }

        return fileSystemReceivers.RunReceiver<THandler>(receivers, cancellationToken);
    }
}
