namespace Conqueror.Transport.Http.Tests.Signalling.Sse;

[TestFixture]
public sealed partial class HttpSseSignalTcpTests
{
    private static readonly object[] HttpVersionTestCases =
    [
        new object[] { HttpProtocols.Http1, HttpVersion.Version11, HttpVersionPolicy.RequestVersionExact },
        new object[] { HttpProtocols.Http2, HttpVersion.Version20, HttpVersionPolicy.RequestVersionExact },
        new object[]
        {
            HttpProtocols.Http1AndHttp2,
            HttpVersion.Version20,
            HttpVersionPolicy.RequestVersionOrLower, // because we are not using HTTPS here, this will cause a downgrade to HTTP/1.1
        },
    ];

    [Test]
    [TestCaseSource(nameof(HttpVersionTestCases))]
    public async Task GivenWebAppListeningOnTcp_WhenConnectingToSseSignalEndpoint_ReturnsItems(
        HttpProtocols serverProtocols,
        Version clientHttpVersion,
        HttpVersionPolicy clientVersionPolicy
    )
    {
        var testTimeout = TimeSpan.FromSeconds(value: 2);
        using var testTimeoutCts = new CancellationTokenSource(testTimeout);
        var testTimeoutToken = testTimeoutCts.Token;

        var builder = WebApplication.CreateBuilder();

        _ = builder.Logging.ClearProviders().AddTestLogger().SetMinimumLevel(LogLevel.Trace);

        _ = builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(
                IPAddress.Loopback,
                port: 0,
                listenOptions =>
                {
                    listenOptions.Protocols = serverProtocols;
                }
            );
        });

        _ = builder.Services.AddConquerorHttpServerAspNetCore();

        await using var app = builder.Build();

        _ = app.UseConquerorWellKnownErrorHandling();
        _ = app.MapServerSentEventsSignalsEndpoint("signals");

        await app.StartAsync(testTimeoutToken);

        Uri? listenAddress = null;

        if (app.Services.GetService<IServer>()?.Features.Get<IServerAddressesFeature>() is { } saf)
        {
            listenAddress = new Uri(saf.Addresses.First());
        }

        Assert.That(listenAddress, Is.Not.Null);

        var receivedSignals = new Queue<TestSignal>();

        var clientServices = new ServiceCollection()
            .AddConquerorHttpClient()
            .AddSignalHandler(new TestSignalHandler(receivedSignals))
            .AddSingleton<Action<IHttpSseSignalReceiver>>(r =>
                r.Enable(new(listenAddress, "signals")).WithHttpVersion(clientHttpVersion, clientVersionPolicy)
            )
            .BuildServiceProvider();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testTimeoutToken);

        await using var handle = clientServices
            .GetRequiredService<ISignalReceivers>()
            .RunHttpSseSignalReceiver<TestSignalHandler>(cts.Token);

        await handle.InitialConnectionTask;

        var signal1 = new TestSignal(Payload: 1);
        var signal2 = new TestSignal(Payload: 2);

        var publisher = app
            .Services.GetRequiredService<ISignalPublishers>()
            .For(TestSignal.T)
            .WithTransport(b => b.UseHttpServerSentEvents());

        await publisher.Handle(signal1, testTimeoutToken);
        await publisher.Handle(signal2, testTimeoutToken);

        // give receiver time to receive the signals
        await Task.Delay(TimeSpan.FromMilliseconds(value: 100), TimeProvider.System, cts.Token);

        Assert.That(receivedSignals, Is.EqualTo([signal1, signal2]));

        await cts.CancelAsync();

        await app.StopAsync(testTimeoutToken);
    }

    [HttpSseSignal]
    private sealed partial record TestSignal(int Payload);

    private sealed partial class TestSignalHandler(Queue<TestSignal> receivedSignals) : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            receivedSignals.Enqueue(signal);
        }

        public static void ConfigureHttpSseReceiver(IHttpSseSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IHttpSseSignalReceiver>>().Invoke(receiver);
    }
}
