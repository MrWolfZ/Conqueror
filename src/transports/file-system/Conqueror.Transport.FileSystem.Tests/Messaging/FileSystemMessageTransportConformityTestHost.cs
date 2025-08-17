namespace Conqueror.Transport.FileSystem.Tests.Messaging;

public sealed class FileSystemMessageTransportConformityTestHost : IMessageTransportConformityTestHost
{
    private readonly DirectoryInfo baseDirectory;
    private readonly List<FileSystemMessageTransportConformityReceiverTestHost> receiverHosts = [];

    private readonly ServiceProvider serviceProvider = new ServiceCollection()
        .AddLogging(l => l.AddTestLogger().SetMinimumLevel(LogLevel.Trace))
        .BuildServiceProvider();

    private readonly FileSystemTransportTestTimeouts timeouts = FileSystemTransportTestTimeouts.Create();

    private FileSystemMessageTransportConformitySenderTestHost? senderHost;

    private FileSystemMessageTransportConformityTestHost(
        FileSystemMessageConformityTestCase testCase,
        DirectoryInfo baseDirectory
    )
    {
        TestCase = testCase;

        this.baseDirectory = baseDirectory;
    }

    private FileSystemMessageConformityTestCase TestCase { get; }

    public FileSystemMessageTransportConformitySenderTestHost SenderHost =>
        senderHost ?? throw new InvalidOperationException("publisher host not created");

    public IReadOnlyCollection<FileSystemMessageTransportConformityReceiverTestHost> ReceiverHosts => receiverHosts;

    public ConcurrentQueue<Exception?> ReceiverConfigurationExceptions { get; } = [];

    public CancellationToken TestTimeoutToken => timeouts.TestTimeoutToken;

    public TimeSpan AssertionTimeout => timeouts.AssertionTimeout;

    public TimeSpan ShortDelay => timeouts.ShortDelay;

    public int AssertionTimeoutInMs => timeouts.AssertionTimeoutInMs;

    public ILogger Logger =>
        serviceProvider.GetRequiredService<ILogger<FileSystemMessageTransportConformityTestHost>>();

    async Task<IMessageTransportConformityReceiverTestHost> IMessageTransportConformityTestHost.CreateReceiverTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? messageCallback
    ) => await CreateReceiverTestHost(cancellationToken, messageCallback: messageCallback);

    async Task<IMessageTransportConformitySenderTestHost> IMessageTransportConformityTestHost.CreateSenderTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? sendCallback
    ) => await CreateSenderTestHost(sendCallback);

    public async ValueTask DisposeAsync()
    {
        timeouts.Dispose();

        await serviceProvider.DisposeAsync();

        if (senderHost is not null)
        {
            await senderHost.DisposeAsync();
        }

        foreach (var receiverHost in receiverHosts)
        {
            await receiverHost.DisposeAsync();
        }

        if (baseDirectory.Exists)
        {
            var attempts = 0;

            while (true)
            {
                // there are some rare edge cases where a receiver might still be accessing a file
                // even after it was disposed, so we work around that by retrying the deletion of
                // the test data dir a few times
                try
                {
                    baseDirectory.Delete(recursive: true);

                    return;
                }
                catch when (attempts <= 10)
                {
                    attempts += 1;

                    await Task.Delay(millisecondsDelay: 1, CancellationToken.None);
                }
            }
        }
    }

    public static FileSystemMessageTransportConformityTestHost Create(FileSystemMessageConformityTestCase testCase) =>
        new(testCase, FileSystemTestDirectory.Create());

    private async Task<FileSystemMessageTransportConformityReceiverTestHost> CreateReceiverTestHost(
        CancellationToken cancellationToken,
        Action<IServiceCollection>? configureServices = null,
        Func<object, ConquerorContext, CancellationToken, Task>? messageCallback = null
    )
    {
        var receiverHost = await FileSystemMessageTransportConformityReceiverTestHost.CreateReceiverHost(
            this,
            TestCase,
            configureServices,
            baseDirectory,
            messageCallback,
            h => receiverHosts.Remove(h),
            cancellationToken
        );

        receiverHosts.Add(receiverHost);

        return receiverHost;
    }

    private Task<FileSystemMessageTransportConformitySenderTestHost> CreateSenderTestHost(
        Func<object, ConquerorContext, CancellationToken, Task>? sendCallback = null
    )
    {
        if (senderHost is not null)
        {
            throw new InvalidOperationException("sender host already created");
        }

        senderHost = FileSystemMessageTransportConformitySenderTestHost.CreateSenderHost(
            this,
            TestCase,
            baseDirectory,
            sendCallback
        );

        return Task.FromResult(senderHost);
    }
}
