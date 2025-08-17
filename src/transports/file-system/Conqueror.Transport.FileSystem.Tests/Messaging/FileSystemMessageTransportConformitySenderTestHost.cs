namespace Conqueror.Transport.FileSystem.Tests.Messaging;

public sealed class FileSystemMessageTransportConformitySenderTestHost : IMessageTransportConformitySenderTestHost
{
    private readonly ServiceProvider serviceProvider;

    private FileSystemMessageTransportConformitySenderTestHost(ServiceProvider serviceProvider) =>
        this.serviceProvider = serviceProvider;

    public IMessageSenders MessageSenders => serviceProvider.GetRequiredService<IMessageSenders>();

    public IConquerorContextAccessor ConquerorContextAccessor =>
        serviceProvider.GetRequiredService<IConquerorContextAccessor>();

    public async ValueTask DisposeAsync() => await serviceProvider.DisposeAsync();

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "false positive, service provider is disposed by the test host"
    )]
    public static FileSystemMessageTransportConformitySenderTestHost CreateSenderHost(
        FileSystemMessageTransportConformityTestHost host,
        FileSystemMessageConformityTestCase testCase,
        DirectoryInfo baseDirectory,
        Func<object, ConquerorContext, CancellationToken, Task>? sendCallback
    )
    {
        var services = new ServiceCollection();

        _ = services
            .AddConquerorFileSystemTransport()
            .AddSingleton(baseDirectory)
            .AddSingleton(host.Logger)
            .AddTransient(typeof(FileSystemMessageTestCases.TestMessageMiddleware<,>));

        if (sendCallback is not null)
        {
            _ = services.AddSingleton(sendCallback);
        }

        testCase.RegisterClientServices(services);

        var p = services.BuildServiceProvider();

        var receiverHost = new FileSystemMessageTransportConformitySenderTestHost(p);

        return receiverHost;
    }

    public T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();
}
