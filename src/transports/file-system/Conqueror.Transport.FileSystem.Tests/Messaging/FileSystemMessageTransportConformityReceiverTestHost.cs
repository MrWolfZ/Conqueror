using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.FileSystem.Tests.Messaging;

public delegate Task FnToCallFromHandler(object message, CancellationToken cancellationToken);

public sealed class FileSystemMessageTransportConformityReceiverTestHost : IMessageTransportConformityReceiverTestHost
{
    private readonly Action onDisposeOrCancel;

    private FileSystemMessageTransportConformityReceiverTestHost(Action onDisposeOrCancel)
    {
        this.onDisposeOrCancel = onDisposeOrCancel;
    }

    private FileSystemTransportTestHost FileSystemTransportTestHost { get; init; } = null!;

    public required ReceiverExecutionHandle? ReceiverExecutionHandle { get; init; }

    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "by design")]
    public static async Task<FileSystemMessageTransportConformityReceiverTestHost> CreateReceiverHost(
        FileSystemMessageTransportConformityTestHost host,
        FileSystemMessageConformityTestCase testCase,
        Action<IServiceCollection>? configureServices,
        DirectoryInfo baseDirectory,
        Func<object, ConquerorContext, CancellationToken, Task>? messageCallback,
        Action<FileSystemMessageTransportConformityReceiverTestHost> onDisposeOrCancel,
        CancellationToken cancellationToken)
    {
        FileSystemMessageTransportConformityReceiverTestHost? receiverHost = null;

        var reg = cancellationToken.Register(() =>
        {
            if (receiverHost is not null)
            {
                onDisposeOrCancel(receiverHost);
            }
        });

        var fileSystemTransportTestHost = await FileSystemTransportTestHost.Create(services =>
        {
            _ = services.AddConquerorFileSystemTransport()
                        .AddSingleton<Action<IFileSystemMessageReceiver>>(r => ConfigureReceiver(
                                                                              host,
                                                                              testCase,
                                                                              baseDirectory,
                                                                              r))
                        .AddSingleton(baseDirectory)
                        .AddSingleton(ILogger (p) => p.GetRequiredService<ILogger<FileSystemMessageTransportConformityTestHost>>())
                        .AddSingleton<FnToCallFromHandler>(p => (s, ct) => messageCallback?.Invoke(
                                                                               s,
                                                                               p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                                                               ct)
                                                                           ?? Task.CompletedTask);

            testCase.RegisterHandler(services);

            testCase.RegisterServerServices(services);

            configureServices?.Invoke(services);
        });

        var executionHandle = testCase.RunReceivers(fileSystemTransportTestHost.Resolve<IMessageReceivers>(), cancellationToken);

        receiverHost = new(() =>
        {
            reg.Dispose();

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
        where T : notnull
        => FileSystemTransportTestHost.Resolve<T>();

    public async ValueTask DisposeAsync()
    {
        onDisposeOrCancel();

        if (ReceiverExecutionHandle is not null)
        {
            await ReceiverExecutionHandle.DisposeAsync();
        }

        await FileSystemTransportTestHost.DisposeAsync();
    }

    private static void ConfigureReceiver(
        FileSystemMessageTransportConformityTestHost host,
        FileSystemMessageConformityTestCase testCase,
        DirectoryInfo baseDirectory,
        IFileSystemMessageReceiver receiver)
    {
        _ = receiver.EnableMultipleCompetingInstances(baseDirectory.FullName, leaseDuration: TimeSpan.FromSeconds(10), pollingInterval: TimeSpan.FromMilliseconds(10))
                    .WithMessageCallback(s => host.Logger.LogInformation("signal callback: {Message}", s))
                    .WithExceptionCallback(ex => host.Logger.LogError(ex, "exception callback"));

        testCase.ConfigureReceiver(host, receiver);
    }
}
