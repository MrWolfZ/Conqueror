namespace Conqueror.Transport.FileSystem.Tests.Messaging;

public delegate Task FnToCallFromHandler(object message, CancellationToken cancellationToken);

public sealed class FileSystemMessageTransportConformityReceiverTestHost : IMessageTransportConformityReceiverTestHost
{
    private readonly Func<Task> onDisposeOrCancel;

    private FileSystemMessageTransportConformityReceiverTestHost(Action onDisposeOrCancel) =>
        this.onDisposeOrCancel = () =>
        {
            onDisposeOrCancel();
            return Task.CompletedTask;
        };

    private FileSystemMessageTransportConformityReceiverTestHost(Func<Task> onDisposeOrCancel) =>
        this.onDisposeOrCancel = onDisposeOrCancel;

    private FileSystemTransportTestHost FileSystemTransportTestHost { get; init; } = null!;

    public required ReceiverExecutionHandle? ReceiverExecutionHandle { get; init; }

    public async ValueTask DisposeAsync()
    {
        await onDisposeOrCancel();

        if (ReceiverExecutionHandle is not null)
        {
            await ReceiverExecutionHandle.DisposeAsync();
        }

        await FileSystemTransportTestHost.DisposeAsync();
    }

    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "by design")]
    public static async Task<FileSystemMessageTransportConformityReceiverTestHost> CreateReceiverHost(
        FileSystemMessageTransportConformityTestHost host,
        FileSystemMessageConformityTestCase testCase,
        Action<IServiceCollection>? configureServices,
        DirectoryInfo baseDirectory,
        Func<object, ConquerorContext, CancellationToken, Task>? messageCallback,
        Action<FileSystemMessageTransportConformityReceiverTestHost> onDisposeOrCancel,
        CancellationToken cancellationToken
    )
    {
        FileSystemMessageTransportConformityReceiverTestHost? receiverHost = null;

        var reg = cancellationToken.Register(() =>
        {
            if (receiverHost is not null)
            {
                onDisposeOrCancel(receiverHost);
            }
        });

        var fileSystemTransportTestHost = await FileSystemTransportTestHost.Create(
            services =>
            {
                _ = services
                    .AddConquerorFileSystemTransport()
                    .AddSingleton<Action<IFileSystemMessageReceiver>>(r =>
                        ConfigureReceiver(host, testCase, baseDirectory, r)
                    )
                    .AddSingleton(baseDirectory)
                    .AddSingleton(ILogger (p) => p.GetRequiredService<ILogger<FileSystemMessageTransportConformityTestHost>>()
                    )
                    .AddSingleton<FnToCallFromHandler>(p =>
                        (s, ct) =>
                            messageCallback?.Invoke(
                                s,
                                p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                ct
                            ) ?? Task.CompletedTask
                    );

                testCase.RegisterHandler(services);

                testCase.RegisterServerServices(services);

                configureServices?.Invoke(services);
            },
            cancellationToken
        );

        var executionHandle = testCase.RunReceivers(
            fileSystemTransportTestHost.Resolve<IMessageReceivers>(),
            cancellationToken
        );

        receiverHost = new FileSystemMessageTransportConformityReceiverTestHost(async () =>
        {
            await reg.DisposeAsync();

            if (receiverHost is not null)
            {
                onDisposeOrCancel(receiverHost);
            }
        })
        {
            FileSystemTransportTestHost = fileSystemTransportTestHost,
            ReceiverExecutionHandle = executionHandle,
        };

        return receiverHost;
    }

    public T Resolve<T>()
        where T : notnull => FileSystemTransportTestHost.Resolve<T>();

    private static void ConfigureReceiver(
        FileSystemMessageTransportConformityTestHost host,
        FileSystemMessageConformityTestCase testCase,
        DirectoryInfo baseDirectory,
        IFileSystemMessageReceiver receiver
    )
    {
        _ = receiver
            .EnableMultipleCompetingInstances(
                baseDirectory.FullName,
                TimeSpan.FromSeconds(value: 10),
                TimeSpan.FromMilliseconds(value: 10)
            )
            .WithMessageCallback(s => host.Logger.LogInformation("signal callback: {Message}", s))
            .WithExceptionCallback(ex => host.Logger.LogError(ex, "exception callback"));

        testCase.ConfigureReceiver(host, receiver);
    }
}
