namespace Conqueror.Transport.FileSystem.Tests.Signalling;

public sealed class FileSystemSignalTransportConformityTestHost : ISignalTransportConformityTestHost
{
    private readonly DirectoryInfo baseDirectory;
    private readonly List<FileSystemSignalTransportConformityReceiverTestHost> receiverHosts = [];

    private readonly ServiceProvider serviceProvider = new ServiceCollection()
        .AddLogging(l => l.AddTestLogger().SetMinimumLevel(LogLevel.Trace))
        .BuildServiceProvider();

    private readonly FileSystemTransportTestTimeouts timeouts = FileSystemTransportTestTimeouts.Create();

    private FileSystemSignalTransportConformityPublisherTestHost? publisherHost;

    private FileSystemSignalTransportConformityTestHost(
        FileSystemSignalConformityTestCase testCase,
        DirectoryInfo baseDirectory
    )
    {
        TestCase = testCase;

        this.baseDirectory = baseDirectory;
    }

    private FileSystemSignalConformityTestCase TestCase { get; }

    public FileSystemSignalTransportConformityPublisherTestHost PublisherHost =>
        publisherHost ?? throw new InvalidOperationException("publisher host not created");

    public IReadOnlyCollection<FileSystemSignalTransportConformityReceiverTestHost> ReceiverHosts => receiverHosts;

    public int DelegateHandlerCount { get; set; }

    public TimeSpan TestTimeout => timeouts.TestTimeout;

    public ConcurrentQueue<Exception?> ReceiverConfigurationExceptions { get; } = [];

    public CancellationToken TestTimeoutToken => timeouts.TestTimeoutToken;

    public TimeSpan AssertionTimeout => timeouts.AssertionTimeout;

    public TimeSpan ShortDelay => timeouts.ShortDelay;

    public int AssertionTimeoutInMs => timeouts.AssertionTimeoutInMs;

    public ILogger Logger => serviceProvider.GetRequiredService<ILogger<FileSystemSignalTransportConformityTestHost>>();

    async Task<ISignalTransportConformityReceiverTestHost> ISignalTransportConformityTestHost.CreateReceiverTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? signalCallback
    ) => await CreateReceiverTestHost(cancellationToken, signalCallback);

    async Task<ISignalTransportConformityPublisherTestHost> ISignalTransportConformityTestHost.CreatePublisherTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? publishCallback
    ) => await CreatePublisherTestHost(cancellationToken, publishCallback);

    public async ValueTask DisposeAsync()
    {
        timeouts.Dispose();

        await serviceProvider.DisposeAsync();

        foreach (var receiverHost in receiverHosts)
        {
            await receiverHost.DisposeAsync();
        }

        if (publisherHost is not null)
        {
            await publisherHost.DisposeAsync();
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

    public static FileSystemSignalTransportConformityTestHost Create(FileSystemSignalConformityTestCase testCase) =>
        new(testCase, FileSystemTestDirectory.Create());

    private Task<FileSystemSignalTransportConformityReceiverTestHost> CreateReceiverTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? signalCallback = null
    )
    {
        var receiverHost = FileSystemSignalTransportConformityReceiverTestHost.CreateReceiverHost(
            this,
            TestCase,
            baseDirectory,
            signalCallback,
            h => receiverHosts.Remove(h),
            cancellationToken
        );

        receiverHosts.Add(receiverHost);

        return Task.FromResult(receiverHost);
    }

    public async Task<FileSystemSignalTransportConformityPublisherTestHost> CreatePublisherTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? publishCallback = null
    )
    {
        if (publisherHost is not null)
        {
            throw new InvalidOperationException("publisher host already created");
        }

        publisherHost = await FileSystemSignalTransportConformityPublisherTestHost.CreatePublisherHost(
            TestCase,
            baseDirectory,
            publishCallback,
            cancellationToken
        );

        return publisherHost;
    }
}
