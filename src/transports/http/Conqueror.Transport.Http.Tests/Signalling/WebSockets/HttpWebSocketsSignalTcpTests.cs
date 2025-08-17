namespace Conqueror.Transport.Http.Tests.Signalling.WebSockets;

[TestFixture]
public sealed partial class HttpWebSocketsSignalTcpTests
{
    [Test]
    public async Task GivenWebAppListeningOnTcp_WhenConnectingToWebSocketsSignalEndpoint_ReturnsItems()
    {
        var testTimeout = TimeSpan.FromSeconds(value: 2);
        using var testTimeoutCts = new CancellationTokenSource(testTimeout);
        var testTimeoutToken = testTimeoutCts.Token;

        var builder = WebApplication.CreateBuilder();

        _ = builder.Logging.ClearProviders().AddTestLogger().SetMinimumLevel(LogLevel.Trace);

        _ = builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port: 0));

        _ = builder.Services.AddConquerorHttpServerAspNetCore();

        await using var app = builder.Build();

        _ = app.UseWebSockets();
        _ = app.UseConquerorWellKnownErrorHandling();
        _ = app.MapWebSocketsSignalsEndpoint("signals");

        await app.StartAsync(testTimeoutToken);

        Uri? listenAddress = null;

        if (app.Services.GetService<IServer>()?.Features.Get<IServerAddressesFeature>() is { } saf)
        {
            listenAddress = new Uri(saf.Addresses.First());
        }

        Assert.That(listenAddress, Is.Not.Null);

        var webSocketAddress = new Uri(
            listenAddress.AbsoluteUri.Replace("http://", "ws://", StringComparison.Ordinal) + "signals"
        );

        var receivedSignals = new Queue<TestSignal>();

        var clientServices = new ServiceCollection()
            .AddConquerorHttpClient()
            .AddSignalHandler(new TestSignalHandler(receivedSignals))
            .AddSingleton<Action<IHttpWebSocketsSignalReceiver>>(r => r.Enable(webSocketAddress))
            .BuildServiceProvider();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(testTimeoutToken);

        await using var handle = clientServices
            .GetRequiredService<ISignalReceivers>()
            .RunHttpWebSocketsSignalReceiver<TestSignalHandler>(cts.Token);

        await handle.InitialConnectionTask;

        var signal1 = new TestSignal(Payload: 1);
        var signal2 = new TestSignal(Payload: 2);

        var publisher = app
            .Services.GetRequiredService<ISignalPublishers>()
            .For(TestSignal.T)
            .WithTransport(b => b.UseHttpWebSockets());

        await publisher.Handle(signal1, testTimeoutToken);
        await publisher.Handle(signal2, testTimeoutToken);

        // give receiver time to receive the signals
        await Task.Delay(TimeSpan.FromMilliseconds(value: 100), TimeProvider.System, testTimeoutToken);

        Assert.That(receivedSignals, Is.EqualTo([signal1, signal2]));

        await cts.CancelAsync();

        await app.StopAsync(testTimeoutToken);
    }

    [HttpWebSocketsSignal]
    private sealed partial record TestSignal(int Payload);

    private sealed partial class TestSignalHandler(Queue<TestSignal> receivedSignals) : TestSignal.IHandler
    {
        public async Task Handle(TestSignal signal, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            receivedSignals.Enqueue(signal);
        }

        public static void ConfigureHttpWebSocketsReceiver(IHttpWebSocketsSignalReceiver receiver) =>
            receiver.ServiceProvider.GetRequiredService<Action<IHttpWebSocketsSignalReceiver>>().Invoke(receiver);
    }
}
