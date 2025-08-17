namespace Conqueror.Transport.FileSystem.Tests.Signalling;

public sealed class FileSystemSignalTransportConformityPublisherTestHost : ISignalTransportConformityPublisherTestHost
{
    private FileSystemTransportTestHost FileSystemTransportTestHost { get; set; } = null!;

    public ISignalPublishers SignalPublishers => FileSystemTransportTestHost.Resolve<ISignalPublishers>();

    public IConquerorContextAccessor ConquerorContextAccessor =>
        FileSystemTransportTestHost.Resolve<IConquerorContextAccessor>();

    public async ValueTask DisposeAsync() => await FileSystemTransportTestHost.DisposeAsync();

    [SuppressMessage("Style", "IDE0017:Simplify object initialization", Justification = "false positive")]
    public static async Task<FileSystemSignalTransportConformityPublisherTestHost> CreatePublisherHost(
        FileSystemSignalConformityTestCase testCase,
        DirectoryInfo baseDirectory,
        Func<object, ConquerorContext, CancellationToken, Task>? publishCallback,
        CancellationToken cancellationToken
    )
    {
        var host = new FileSystemSignalTransportConformityPublisherTestHost();

        host.FileSystemTransportTestHost = await FileSystemTransportTestHost.Create(
            services =>
            {
                testCase.RegisterOnPublisher?.Invoke(services);

                _ = services
                    .AddConquerorFileSystemTransport()
                    .AddSingleton(baseDirectory)
                    .AddSingleton(
                        ILogger (p) => p.GetRequiredService<ILogger<FileSystemSignalTransportConformityTestHost>>()
                    );

                if (publishCallback is not null)
                {
                    _ = services.AddSingleton(publishCallback);
                }

                testCase.RegisterServerServices(services);
            },
            cancellationToken
        );

        return host;
    }

    public T Resolve<T>()
        where T : notnull => FileSystemTransportTestHost.Resolve<T>();
}
