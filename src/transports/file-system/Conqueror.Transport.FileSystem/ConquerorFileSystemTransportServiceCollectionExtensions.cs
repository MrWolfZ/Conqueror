using Conqueror;
using Conqueror.Transport.FileSystem;
using Conqueror.Transport.FileSystem.Messaging;
using Conqueror.Transport.FileSystem.Signalling;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace (it's a convention to place service collection extensions in this namespace)
namespace Microsoft.Extensions.DependencyInjection;

public static class ConquerorFileSystemTransportServiceCollectionExtensions
{
    public static IServiceCollection AddConquerorFileSystemTransport(this IServiceCollection services)
    {
        _ = services.AddConqueror();

        services.TryAddSingleton<FileSystemStores>();

        AddMessaging(services);
        AddSignalling(services);

        return services;
    }

    private static void AddMessaging(IServiceCollection services)
    {
        services.TryAddSingleton<IFileSystemMessageSenderFactory, FileSystemMessageSenderFactory>();
        services.TryAddSingleton<IFileSystemMessageReceivers, FileSystemMessageReceivers>();
        services.TryAddSingleton<FileSystemMessageReceiverFactory>();
        services.TryAddSingleton<FileSystemMessageReceiverRunner>();
    }

    private static void AddSignalling(IServiceCollection services)
    {
        services.TryAddSingleton<IFileSystemSignalPublisherFactory, FileSystemSignalPublisherFactory>();
        services.TryAddSingleton<IFileSystemSignalReceivers, FileSystemSignalReceivers>();
        services.TryAddSingleton<FileSystemSignalReceiverFactory>();
        services.TryAddSingleton<FileSystemSignalReceiverRunner>();
    }
}
