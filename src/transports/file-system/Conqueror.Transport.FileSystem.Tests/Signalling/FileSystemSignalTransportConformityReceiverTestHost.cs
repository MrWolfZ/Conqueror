namespace Conqueror.Transport.FileSystem.Tests.Signalling;

public delegate Task FnToCallFromHandler(object signal, CancellationToken cancellationToken);

public sealed class FileSystemSignalTransportConformityReceiverTestHost : ISignalTransportConformityReceiverTestHost
{
    private readonly Func<FileSystemSignalTransportConformityReceiverTestHost, Task> onDispose;
    private readonly ServiceProvider serviceProvider;

    private FileSystemSignalTransportConformityReceiverTestHost(
        ServiceProvider serviceProvider,
        Func<FileSystemSignalTransportConformityReceiverTestHost, Task> onDispose
    )
    {
        this.serviceProvider = serviceProvider;
        this.onDispose = onDispose;
    }

    public required ReceiverExecutionHandle? ReceiverExecutionHandle { get; init; }

    public async ValueTask DisposeAsync()
    {
        await onDispose(this);

        if (ReceiverExecutionHandle is not null)
        {
            await ReceiverExecutionHandle.DisposeAsync();
        }

        await serviceProvider.DisposeAsync();
    }

    public static FileSystemSignalTransportConformityReceiverTestHost CreateReceiverHost(
        FileSystemSignalTransportConformityTestHost host,
        FileSystemSignalConformityTestCase testCase,
        DirectoryInfo baseDirectory,
        Func<object, ConquerorContext, CancellationToken, Task>? signalCallback,
        Action<FileSystemSignalTransportConformityReceiverTestHost> onDisposeOrCancel,
        CancellationToken cancellationToken
    )
    {
        var services = new ServiceCollection();

        _ = services
            .AddConquerorFileSystemTransport()
            .AddSingleton<Action<IFileSystemSignalReceiver>>(r => ConfigureReceiver(host, testCase, baseDirectory, r))
            .AddSingleton(host.Logger)
            .AddTransient(typeof(FileSystemSignalTestCases.TestSignalMiddleware<>))
            .AddSingleton(baseDirectory)
            .AddSingleton<FnToCallFromHandler>(p =>
                (s, ct) =>
                    signalCallback?.Invoke(s, p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!, ct)
                    ?? Task.CompletedTask
            );

        testCase.RegisterHandler(services);

        testCase.RegisterClientServices(services);

        var p = services.BuildServiceProvider();

        var executionHandle = testCase.RunReceivers(p.GetRequiredService<ISignalReceivers>(), cancellationToken);

        CancellationTokenRegistration? reg = null;

        var receiverHost = new FileSystemSignalTransportConformityReceiverTestHost(
            p,
            async h =>
            {
                // ReSharper disable once AccessToModifiedClosure
                if (reg is not null)
                {
                    // ReSharper disable once AccessToModifiedClosure
                    await reg.Value.DisposeAsync();
                }

                onDisposeOrCancel(h);
            }
        )
        {
            ReceiverExecutionHandle = executionHandle,
        };

        reg = cancellationToken.Register(
            h => onDisposeOrCancel((FileSystemSignalTransportConformityReceiverTestHost)h!),
            receiverHost
        );

        return receiverHost;
    }

    public T Resolve<T>()
        where T : notnull => serviceProvider.GetRequiredService<T>();

    private static void ConfigureReceiver(
        FileSystemSignalTransportConformityTestHost host,
        FileSystemSignalConformityTestCase testCase,
        DirectoryInfo baseDirectory,
        IFileSystemSignalReceiver receiver
    )
    {
        testCase.ConfigureReceiver(host, receiver);

        if (receiver.HandlerType is null)
        {
            host.DelegateHandlerCount += 1;
        }

        _ = receiver
            .EnableMultipleCompetingInstances(
                baseDirectory.FullName,
                host.TestTimeout,
                TimeSpan.FromMilliseconds(value: 10)
            )
            .WithName(receiver.HandlerType?.Name ?? $"delegate-{host.DelegateHandlerCount}")
            .WithSignalCallback(s =>
                host.Logger.LogInformation(
                    "signal callback for handler '{HandlerType}': {Signal}",
                    receiver.HandlerType?.Name,
                    s
                )
            )
            .WithExceptionCallback(ex => host.Logger.LogError(ex, "exception callback"));
    }
}
