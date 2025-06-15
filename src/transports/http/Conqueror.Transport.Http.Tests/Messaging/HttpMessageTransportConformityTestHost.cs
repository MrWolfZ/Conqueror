using Conqueror.Transport.ConformityTests.Messaging;

namespace Conqueror.Transport.Http.Tests.Messaging;

public sealed class HttpMessageTransportConformityTestHost : IMessageTransportConformityTestHost
{
    private readonly ServiceProvider serviceProvider = new ServiceCollection().AddLogging(l => l.AddTestLogger().SetMinimumLevel(LogLevel.Trace))
                                                                              .BuildServiceProvider();

    private readonly HttpTransportTestTimeouts timeouts = HttpTransportTestTimeouts.Create();
    private HttpMessageTransportConformityReceiverTestHost? receiverHost;

    private HttpMessageTransportConformitySenderTestHost? senderHost;

    private HttpMessageTransportConformityTestHost(HttpMessageConformityTestCase testCase)
    {
        TestCase = testCase;
    }

    private HttpMessageConformityTestCase TestCase { get; }

    public HttpMessageTransportConformitySenderTestHost SenderHost => senderHost ?? throw new InvalidOperationException("publisher host not created");

    public HttpMessageTransportConformityReceiverTestHost ReceiverHost => receiverHost ?? throw new InvalidOperationException("receiver host not created");

    public CancellationToken TestTimeoutToken => timeouts.TestTimeoutToken;

    public TimeSpan AssertionTimeout => timeouts.AssertionTimeout;

    public TimeSpan ShortDelay => timeouts.ShortDelay;

    public int AssertionTimeoutInMs => timeouts.AssertionTimeoutInMs;

    public ILogger Logger => serviceProvider.GetRequiredService<ILogger<HttpMessageTransportConformityTestHost>>();

    public ConcurrentQueue<Exception?> ReceiverConfigurationExceptions { get; } = new();

    public static HttpMessageTransportConformityTestHost Create(HttpMessageConformityTestCase testCase) => new(testCase);

    public async Task<HttpMessageTransportConformityReceiverTestHost> CreateReceiverTestHost(
        CancellationToken cancellationToken,
        Action<IServiceCollection>? configureServices = null,
        Action<IApplicationBuilder>? configure = null,
        Func<object, ConquerorContext, CancellationToken, Task>? messageCallback = null)
    {
        if (receiverHost is not null)
        {
            throw new InvalidOperationException("receiver host already created");
        }

        receiverHost = await HttpMessageTransportConformityReceiverTestHost.CreateReceiverHost(
            this,
            TestCase,
            configureServices,
            configure,
            messageCallback,
            () => receiverHost = null,
            cancellationToken);

        return receiverHost;
    }

    public Task<HttpMessageTransportConformitySenderTestHost> CreateSenderTestHost(
        Func<object, ConquerorContext, CancellationToken, Task>? sendCallback = null)
    {
        if (senderHost is not null)
        {
            throw new InvalidOperationException("sender host already created");
        }

        senderHost = HttpMessageTransportConformitySenderTestHost.CreateSenderHost(this, TestCase, sendCallback);

        return Task.FromResult(senderHost);
    }

    async Task<IMessageTransportConformityReceiverTestHost> IMessageTransportConformityTestHost.CreateReceiverTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? messageCallback)
        => await CreateReceiverTestHost(cancellationToken, messageCallback: messageCallback);

    async Task<IMessageTransportConformitySenderTestHost> IMessageTransportConformityTestHost.CreateSenderTestHost(
        CancellationToken cancellationToken,
        Func<object, ConquerorContext, CancellationToken, Task>? sendCallback)
        => await CreateSenderTestHost(sendCallback);

    public async ValueTask DisposeAsync()
    {
        timeouts.Dispose();

        await serviceProvider.DisposeAsync();

        if (senderHost != null)
        {
            await senderHost.DisposeAsync();
        }

        if (receiverHost != null)
        {
            await receiverHost.DisposeAsync();
        }
    }
}
