#pragma warning disable IDE0130 // Namespaces don't match folder structure - we want these extensions to be accessible from client registration code without an extra import

namespace Conqueror;

public static class FileSystemSignalPublisherBuilderExtensions
{
    public static IFileSystemSignalPublisher<TSignal> UseFileSystem<TSignal>(
        this SignalPublisherBuilder<TSignal> builder,
        string baseDirectoryPath
    )
        where TSignal : class, IFileSystemSignal<TSignal>
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (
            builder.ServiceProvider.GetService(typeof(IFileSystemSignalPublisherFactory))
            is not IFileSystemSignalPublisherFactory publisherFactory
        )
        {
            throw new InvalidOperationException(
                $"could not resolve '{typeof(IFileSystemSignalPublisherFactory)}'; did you forget to add the Conqueror file system services to the service collection?"
            );
        }

        return publisherFactory.Get<TSignal>(baseDirectoryPath);
    }
}
