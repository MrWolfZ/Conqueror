namespace Conqueror.Transport.Http.Tests.Signalling;

public sealed class HttpSignalTransportConformityTestHost : ISignalTransportConformityTestHost
{
    public static readonly Uri SseAddress = new("http://conqueror.test/api/signals/sse");
    public static readonly Uri WebSocketsAddress = new("ws://localhost/api/signals/ws");

    private readonly List<HttpSignalTransportConformityReceiverTestHost> receiverHosts = [];

    private readonly ServiceProvider serviceProvider = new ServiceCollection()
        .AddLogging(l => l.AddTestLogger().SetMinimumLevel(LogLevel.Trace))
        .BuildServiceProvider();

    private readonly HttpTransportTestTimeouts timeouts = HttpTransportTestTimeouts.Create();
    private HttpSignalTransportConformityPublisherTestHost? publisherHost;

    private HttpSignalTransportConformityTestHost(HttpSignalConformityTestCase testCase) => TestCase = testCase;

    private HttpSignalConformityTestCase TestCase { get; }

    public HttpSignalTransportConformityPublisherTestHost PublisherHost =>
        publisherHost ?? throw new InvalidOperationException("publisher host not created");

    public IReadOnlyCollection<HttpSignalTransportConformityReceiverTestHost> ReceiverHosts => receiverHosts;

    public List<(int StatusCode, string ContentType, bool KeepAlive)?> ServerConnectionResponses { get; } = [];

    public ConcurrentQueue<Exception?> ReceiverConfigurationExceptions { get; } = [];

    public CancellationToken TestTimeoutToken => timeouts.TestTimeoutToken;

    public TimeSpan AssertionTimeout => timeouts.AssertionTimeout;

    public TimeSpan ShortDelay => timeouts.ShortDelay;

    public int AssertionTimeoutInMs => timeouts.AssertionTimeoutInMs;

    public ILogger Logger => serviceProvider.GetRequiredService<ILogger<HttpSignalTransportConformityTestHost>>();

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
    }

    public static HttpSignalTransportConformityTestHost Create(HttpSignalConformityTestCase testCase) => new(testCase);

    public Task<WebSocket> ConnectToWebSocket(Uri address, Action<IHeaderDictionary>? configureHeaders = null) =>
        PublisherHost.ConnectToWebSocket(address, configureHeaders);

    public Task TriggerReconnect() => PublisherHost.TriggerReconnect();

    public Task<HttpSignalTransportConformityReceiverTestHost> CreateReceiverTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? signalCallback = null,
        Func<CancellationToken, Task>? reconnectDelayCallback = null
    )
    {
        var receiverHost = HttpSignalTransportConformityReceiverTestHost.CreateReceiverHost(
            this,
            TestCase,
            signalCallback,
            reconnectDelayCallback,
            h => receiverHosts.Remove(h),
            cancellationToken
        );

        receiverHosts.Add(receiverHost);

        return Task.FromResult(receiverHost);
    }

    public async Task<HttpSignalTransportConformityPublisherTestHost> CreatePublisherTestHost(
        CancellationToken _,
        Func<object, ConquerorContext, CancellationToken, Task>? publishCallback = null
    )
    {
        if (publisherHost is not null)
        {
            throw new InvalidOperationException("publisher host already created");
        }

        publisherHost = await HttpSignalTransportConformityPublisherTestHost.CreatePublisherHost(
            TestCase,
            publishCallback
        );

        foreach (var response in ServerConnectionResponses)
        {
            publisherHost.ServerConnectionResponses.Enqueue(response);
        }

        return publisherHost;
    }
}
