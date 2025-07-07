namespace Conqueror.Transport.FileSystem.Tests.Signalling;

public delegate Task FnToCallFromHandler(object signal, CancellationToken cancellationToken);

public sealed class FileSystemSignalTransportConformityReceiverTestHost : ISignalTransportConformityReceiverTestHost
{
    private readonly Action<FileSystemSignalTransportConformityReceiverTestHost> onDispose;
    private readonly ServiceProvider serviceProvider;

    private FileSystemSignalTransportConformityReceiverTestHost(
        ServiceProvider serviceProvider,
        Action<FileSystemSignalTransportConformityReceiverTestHost> onDispose)
    {
        this.serviceProvider = serviceProvider;
        this.onDispose = onDispose;
    }

    public required ReceiverExecutionHandle? ReceiverExecutionHandle { get; init; }

    public static FileSystemSignalTransportConformityReceiverTestHost CreateReceiverHost(
        FileSystemSignalTransportConformityTestHost host,
        FileSystemSignalConformityTestCase testCase,
        DirectoryInfo baseDirectory,
        Func<object, ConquerorContext, CancellationToken, Task>? signalCallback,
        Action<FileSystemSignalTransportConformityReceiverTestHost> onDisposeOrCancel,
        CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();

        _ = services.AddConquerorFileSystemTransport()
                    .AddSingleton<Action<IFileSystemSignalReceiver>>(r => ConfigureReceiver(
                                                                         host,
                                                                         testCase,
                                                                         baseDirectory,
                                                                         r))
                    .AddSingleton(host.Logger)
                    .AddTransient(typeof(FileSystemSignalTestCases.TestSignalMiddleware<>))
                    .AddSingleton(baseDirectory)
                    .AddSingleton<FnToCallFromHandler>(p => (s, ct) => signalCallback?.Invoke(
                                                                           s,
                                                                           p.GetRequiredService<IConquerorContextAccessor>().ConquerorContext!,
                                                                           ct)
                                                                       ?? Task.CompletedTask);

        testCase.RegisterHandler(services);

        testCase.RegisterClientServices(services);

        var serviceProvider = services.BuildServiceProvider();

        var executionHandle = testCase.RunReceivers(serviceProvider.GetRequiredService<ISignalReceivers>(), cancellationToken);

        CancellationTokenRegistration? reg = null;

        var receiverHost = new FileSystemSignalTransportConformityReceiverTestHost(
            serviceProvider,
            h =>
            {
                // ReSharper disable once AccessToModifiedClosure
                reg?.Dispose();
                onDisposeOrCancel(h);
            })
        {
            ReceiverExecutionHandle = executionHandle,
        };

        reg = cancellationToken.Register(h => onDisposeOrCancel((FileSystemSignalTransportConformityReceiverTestHost)h!), receiverHost);

        return receiverHost;
    }

    public T Resolve<T>()
        where T : notnull
        => serviceProvider.GetRequiredService<T>();

    public async ValueTask DisposeAsync()
    {
        onDispose(this);

        if (ReceiverExecutionHandle is not null)
        {
            await ReceiverExecutionHandle.DisposeAsync();
        }

        await serviceProvider.DisposeAsync();
    }

    private static void ConfigureReceiver(
        FileSystemSignalTransportConformityTestHost host,
        FileSystemSignalConformityTestCase testCase,
        DirectoryInfo baseDirectory,
        IFileSystemSignalReceiver receiver)
    {
        testCase.ConfigureReceiver(host, receiver);

        if (receiver.HandlerType is null)
        {
            host.DelegateHandlerCount += 1;
        }

        _ = receiver.EnableMultipleCompetingInstances(
                        receiver.HandlerType?.Name ?? "delegate-test-receiver",
                        baseDirectory.FullName,
                        leaseDuration: host.TestTimeout,
                        pollingInterval: TimeSpan.FromMilliseconds(10))
                    .WithName(receiver.HandlerType?.Name ?? $"delegate-{host.DelegateHandlerCount}")
                    .WithSignalCallback(s => host.Logger.LogInformation("signal callback for handler '{HandlerType}': {Signal}", receiver.HandlerType?.Name, s))
                    .WithExceptionCallback(ex => host.Logger.LogError(ex, "exception callback"));
    }
}
